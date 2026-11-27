using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentActionStore
{
    ValueTask SaveAsync(AgentPendingAction action, CancellationToken cancellationToken);

    // Removes the action and returns it. Only one caller gets the action, so it cannot be executed twice
    ValueTask<AgentPendingAction?> TakeAsync(long telegramUserId, string actionId, CancellationToken cancellationToken);
}
