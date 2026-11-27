using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal sealed class TableAgentHistoryStore(TableClient tableClient, TimeProvider timeProvider) : IAgentHistoryStore
{
    // A string property of Azure Table Storage is limited to 32K characters
    private const int MaxStoredTextLength = 30000;

    private const int MaxBatchSize = 100;

    private const string IsUserProperty = "IsUser";

    private const string TextProperty = "Text";

    private readonly SemaphoreSlim tableLock = new(1, 1);

    private volatile bool tableCreated;

    public async ValueTask<IReadOnlyList<AgentHistoryMessage>> GetAsync(
        AgentUserContext user, int maxCount, CancellationToken cancellationToken)
    {
        if (maxCount <= 0)
        {
            return [];
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var messages = new List<AgentHistoryMessage>(maxCount);
        var partitionKey = GetPartitionKey(user);

        // Row keys are inverted timestamps, so the newest messages come first
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
            filter: TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}"),
            maxPerPage: maxCount,
            cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            messages.Add(new(entity.GetBoolean(IsUserProperty) ?? false, entity.GetString(TextProperty).OrEmpty()));
            if (messages.Count >= maxCount)
            {
                break;
            }
        }

        messages.Reverse();
        return messages;
    }

    public async ValueTask AppendAsync(
        AgentUserContext user, string userText, string assistantText, int keepCount, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var partitionKey = GetPartitionKey(user);
        var ticks = timeProvider.GetUtcNow().UtcTicks;

        // The assistant message is newer than the user one, so it gets a smaller inverted key
        TableTransactionAction[] actions =
        [
            new(TableTransactionActionType.UpsertReplace, CreateEntity(partitionKey, ticks, isUser: true, userText)),
            new(TableTransactionActionType.UpsertReplace, CreateEntity(partitionKey, ticks + 1, isUser: false, assistantText))
        ];

        _ = await tableClient.SubmitTransactionAsync(actions, cancellationToken).ConfigureAwait(false);
        await TrimAsync(partitionKey, keepCount, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask TrimAsync(string partitionKey, int keepCount, CancellationToken cancellationToken)
    {
        var obsolete = new List<TableTransactionAction>();
        var index = 0;

        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
            filter: TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}"),
            select: ["PartitionKey", "RowKey"],
            cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (index++ >= keepCount)
            {
                obsolete.Add(new(TableTransactionActionType.Delete, entity));
            }
        }

        foreach (var batch in obsolete.Chunk(MaxBatchSize))
        {
            _ = await tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (tableCreated)
        {
            return;
        }

        await tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (tableCreated is false)
            {
                _ = await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
                tableCreated = true;
            }
        }
        finally
        {
            _ = tableLock.Release();
        }
    }

    private static TableEntity CreateEntity(string partitionKey, long ticks, bool isUser, string text)
        =>
        new(partitionKey, (DateTime.MaxValue.Ticks - ticks).ToString("D19", CultureInfo.InvariantCulture))
        {
            [IsUserProperty] = isUser,
            [TextProperty] = text.Length > MaxStoredTextLength ? text[..MaxStoredTextLength] : text
        };

    private static string GetPartitionKey(AgentUserContext user)
        =>
        string.Create(CultureInfo.InvariantCulture, $"{user.TelegramChatId}_{user.TelegramUserId}");
}
