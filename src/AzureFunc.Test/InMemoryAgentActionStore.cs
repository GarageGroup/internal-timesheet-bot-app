using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet.Test;

internal sealed class InMemoryAgentActionStore : IAgentActionStore
{
    public ConcurrentDictionary<(long UserId, string ActionId), AgentPendingAction> Actions { get; } = new();

    public ValueTask SaveAsync(AgentPendingAction action, CancellationToken cancellationToken)
    {
        Actions[(action.User.TelegramUserId, action.Id)] = action;
        return default;
    }

    public ValueTask<AgentPendingAction?> TakeAsync(long telegramUserId, string actionId, CancellationToken cancellationToken)
        =>
        new(Actions.TryRemove((telegramUserId, actionId), out var action) ? action : null);
}
