using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal sealed class TableAgentActionStore(TableClient tableClient) : IAgentActionStore
{
    private readonly SemaphoreSlim tableLock = new(1, 1);

    private volatile bool tableCreated;

    public async ValueTask SaveAsync(AgentPendingAction action, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var entity = new TableEntity(GetPartitionKey(action.User.TelegramUserId), action.Id)
        {
            ["TelegramChatId"] = action.User.TelegramChatId,
            ["ToolName"] = action.ToolName,
            ["FunctionName"] = action.FunctionName,
            ["ArgumentsJson"] = action.ArgumentsJson,
            ["PreviewJson"] = JsonSerializer.Serialize(action.Arguments),
            ["ExpiresAt"] = action.ExpiresAt
        };

        _ = await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AgentPendingAction?> TakeAsync(long telegramUserId, string actionId, CancellationToken cancellationToken)
    {
        if (IsValidRowKey(actionId) is false)
        {
            return null;
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var partitionKey = GetPartitionKey(telegramUserId);
        var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(
            partitionKey, actionId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response.HasValue is false || response.Value is not TableEntity entity)
        {
            return null;
        }

        try
        {
            // The entity tag makes the removal conditional: a concurrent request that read the same entity fails here
            _ = await tableClient.DeleteEntityAsync(partitionKey, actionId, entity.ETag, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status is 404 or 412)
        {
            return null;
        }

        return new(
            Id: actionId,
            User: new(telegramUserId, entity.GetInt64("TelegramChatId") ?? default),
            ToolName: entity.GetString("ToolName").OrEmpty(),
            FunctionName: entity.GetString("FunctionName").OrEmpty(),
            ArgumentsJson: entity.GetString("ArgumentsJson").OrEmpty(),
            Arguments: JsonSerializer.Deserialize<AgentActionArgument[]>(entity.GetString("PreviewJson").OrNullIfEmpty() ?? "[]") ?? [],
            ExpiresAt: entity.GetDateTimeOffset("ExpiresAt") ?? default);
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

    // The identifier comes from the callback data, so it must not contain characters that are forbidden in a table row key
    private static bool IsValidRowKey(string value)
    {
        if (value.Length is 0 or > 64)
        {
            return false;
        }

        foreach (var symbol in value)
        {
            if (char.IsAsciiLetterOrDigit(symbol) is false)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPartitionKey(long telegramUserId)
        =>
        telegramUserId.ToString(CultureInfo.InvariantCulture);
}
