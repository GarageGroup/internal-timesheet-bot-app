using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class UserIdGetFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> GetUserId(this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.GetUserId(
            static (flowState, userId) => flowState with
            {
                UserId = userId
            });
}