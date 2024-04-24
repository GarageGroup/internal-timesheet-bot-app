using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetGetFlowStep
{
    internal static ChatFlow<TimesheetGetFlowState> GetUserId(this ChatFlow<TimesheetGetFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            UnexpectedFailureUserMessage,
            static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}