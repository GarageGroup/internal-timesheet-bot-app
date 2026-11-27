using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet.Test;

internal sealed class FakeAgentUserTokenProvider(string? accessToken) : IAgentUserTokenProvider
{
    public List<(AgentUserContext User, string Audience)> Requests { get; } = [];

    public ValueTask<string?> GetAccessTokenAsync(AgentUserContext user, string audience, CancellationToken cancellationToken)
    {
        Requests.Add((user, audience));
        return new(accessToken);
    }
}
