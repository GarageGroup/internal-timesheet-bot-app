using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentUserTokenProvider
{
    // Returns an access token of the user for the audience or null if the user has not signed in
    ValueTask<string?> GetAccessTokenAsync(AgentUserContext user, string audience, CancellationToken cancellationToken);
}
