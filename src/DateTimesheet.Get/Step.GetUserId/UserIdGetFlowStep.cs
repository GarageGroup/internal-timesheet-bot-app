using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class UserIdGetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetUserId(this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.GetUserId(
            static (flowState, userId) => flowState with
            {
                UserId = userId
            });
}