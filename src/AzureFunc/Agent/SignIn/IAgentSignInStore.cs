using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentPendingSignIn(
    string State,
    AgentUserContext User,
    string UserDisplayName,
    string CodeVerifier,
    string Locale,
    string SourceUpdateJson,
    DateTimeOffset ExpiresAt)
{
    // The Telegram message that contains the sign-in link. It is removed after the sign-in
    public int? SignInMessageId { get; init; }
}

internal interface IAgentSignInStore
{
    ValueTask SavePendingAsync(AgentPendingSignIn pending, CancellationToken cancellationToken);

    ValueTask<AgentPendingSignIn?> GetPendingAsync(string state, CancellationToken cancellationToken);

    ValueTask DeletePendingAsync(string state, CancellationToken cancellationToken);

    // Returns the protected refresh token of the Telegram user
    ValueTask<string?> GetRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken);

    ValueTask SaveRefreshTokenAsync(long telegramUserId, string protectedRefreshToken, CancellationToken cancellationToken);

    ValueTask DeleteRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken);
}
