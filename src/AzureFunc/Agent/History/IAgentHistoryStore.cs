using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentHistoryMessage(bool IsUser, string Text);

internal interface IAgentHistoryStore
{
    // Returns the latest messages in chronological order
    ValueTask<IReadOnlyList<AgentHistoryMessage>> GetAsync(AgentUserContext user, int maxCount, CancellationToken cancellationToken);

    // Saves a completed turn and removes messages beyond the keepCount latest ones
    ValueTask AppendAsync(AgentUserContext user, string userText, string assistantText, int keepCount, CancellationToken cancellationToken);
}
