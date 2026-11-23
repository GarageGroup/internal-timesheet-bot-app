using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal interface IAgentActionApi
{
    ValueTask<AgentActionDecisionOut> DecideAsync(
        Guid actionId,
        long telegramUpdateId,
        long telegramUserId,
        long telegramChatId,
        AgentActionDecision decision,
        CancellationToken cancellationToken);
}
