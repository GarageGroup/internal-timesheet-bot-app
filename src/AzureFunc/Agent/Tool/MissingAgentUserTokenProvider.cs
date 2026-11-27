using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

// Temporary implementation: user sign-in and token storage are not implemented yet
internal sealed class MissingAgentUserTokenProvider : IAgentUserTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(AgentUserContext user, string audience, CancellationToken cancellationToken)
        =>
        default;
}
