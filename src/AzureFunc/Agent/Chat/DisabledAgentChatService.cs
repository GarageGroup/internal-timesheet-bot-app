using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class DisabledAgentChatService : IAgentChatService
{
    public ValueTask<AgentChatReply> SendAsync(AgentChatRequest request, CancellationToken cancellationToken)
        =>
        throw new InvalidOperationException("The in-bot agent is disabled. Set Agent:Enabled to true to use it.");
}
