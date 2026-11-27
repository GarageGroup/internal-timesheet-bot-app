using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentSignInRequest(AgentUserContext User, string UserDisplayName, string Locale, string SourceUpdateJson);

internal interface IAgentSignInService
{
    // Creates a one-time sign-in link bound to the Telegram user
    ValueTask<Uri> CreateSignInUrlAsync(AgentSignInRequest request, CancellationToken cancellationToken);

    // Returns the sign-in started by the link or null if the state is unknown or expired
    ValueTask<AgentPendingSignIn?> FindPendingAsync(string state, CancellationToken cancellationToken);

    // Exchanges the authorization code and stores the refresh token. Returns null if the sign-in could not be completed
    ValueTask<AgentPendingSignIn?> CompleteAsync(string state, string code, CancellationToken cancellationToken);

    ValueTask SignOutAsync(long telegramUserId, CancellationToken cancellationToken);
}
