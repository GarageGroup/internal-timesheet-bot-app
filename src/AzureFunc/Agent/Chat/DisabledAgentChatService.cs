using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class DisabledAgentChatService : IAgentChatService
{
    private const string Message = "The in-bot agent is disabled. Set Agent:Enabled to true to use it.";

    public ValueTask<AgentChatReply> SendAsync(AgentChatRequest request, CancellationToken cancellationToken)
        =>
        throw new InvalidOperationException(Message);

    public ValueTask<AgentActionOutcome> ConfirmActionAsync(
        AgentUserContext user, string actionId, string locale, CancellationToken cancellationToken)
        =>
        throw new InvalidOperationException(Message);

    public ValueTask<AgentActionOutcome> CancelActionAsync(AgentUserContext user, string actionId, CancellationToken cancellationToken)
        =>
        throw new InvalidOperationException(Message);
}
