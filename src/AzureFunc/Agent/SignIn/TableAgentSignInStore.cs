using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace GarageGroup.Internal.Timesheet;

internal sealed class TableAgentSignInStore(TableClient tableClient) : IAgentSignInStore
{
    private const string PendingPartitionKey = "pending";

    private const string TokenPartitionKey = "token";

    private const string TokenProperty = "Token";

    private readonly SemaphoreSlim tableLock = new(1, 1);

    private volatile bool tableCreated;

    public async ValueTask SavePendingAsync(AgentPendingSignIn pending, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var entity = new TableEntity(PendingPartitionKey, pending.State)
        {
            ["TelegramUserId"] = pending.User.TelegramUserId,
            ["TelegramChatId"] = pending.User.TelegramChatId,
            ["UserDisplayName"] = pending.UserDisplayName,
            ["CodeVerifier"] = pending.CodeVerifier,
            ["Locale"] = pending.Locale,
            ["SourceUpdateJson"] = pending.SourceUpdateJson,
            ["ExpiresAt"] = pending.ExpiresAt
        };

        if (pending.SignInMessageId is int signInMessageId)
        {
            entity["SignInMessageId"] = signInMessageId;
        }

        _ = await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AgentPendingSignIn?> GetPendingAsync(string state, CancellationToken cancellationToken)
    {
        if (IsValidRowKey(state) is false)
        {
            return null;
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(
            PendingPartitionKey, state, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response.HasValue is false || response.Value is not TableEntity entity)
        {
            return null;
        }

        return new(
            State: state,
            User: new(entity.GetInt64("TelegramUserId") ?? default, entity.GetInt64("TelegramChatId") ?? default),
            UserDisplayName: entity.GetString("UserDisplayName").OrEmpty(),
            CodeVerifier: entity.GetString("CodeVerifier").OrEmpty(),
            Locale: entity.GetString("Locale").OrEmpty(),
            SourceUpdateJson: entity.GetString("SourceUpdateJson").OrEmpty(),
            ExpiresAt: entity.GetDateTimeOffset("ExpiresAt") ?? default)
        {
            SignInMessageId = entity.GetInt32("SignInMessageId")
        };
    }

    public async ValueTask DeletePendingAsync(string state, CancellationToken cancellationToken)
    {
        if (IsValidRowKey(state) is false)
        {
            return;
        }

        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);
        _ = await tableClient.DeleteEntityAsync(PendingPartitionKey, state, ETag.All, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<string?> GetRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var response = await tableClient.GetEntityIfExistsAsync<TableEntity>(
            TokenPartitionKey, GetTokenRowKey(telegramUserId), cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.HasValue ? response.Value?.GetString(TokenProperty) : null;
    }

    public async ValueTask SaveRefreshTokenAsync(long telegramUserId, string protectedRefreshToken, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        var entity = new TableEntity(TokenPartitionKey, GetTokenRowKey(telegramUserId))
        {
            [TokenProperty] = protectedRefreshToken
        };

        _ = await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DeleteRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken).ConfigureAwait(false);

        _ = await tableClient.DeleteEntityAsync(
            TokenPartitionKey, GetTokenRowKey(telegramUserId), ETag.All, cancellationToken).ConfigureAwait(false);
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

    // The state comes from the request, so it must not contain characters that are forbidden in a table row key
    private static bool IsValidRowKey(string value)
    {
        if (value.Length is 0 or > 128)
        {
            return false;
        }

        foreach (var symbol in value)
        {
            if (char.IsAsciiLetterOrDigit(symbol) is false && symbol is not ('-' or '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetTokenRowKey(long telegramUserId)
        =>
        telegramUserId.ToString(CultureInfo.InvariantCulture);
}
