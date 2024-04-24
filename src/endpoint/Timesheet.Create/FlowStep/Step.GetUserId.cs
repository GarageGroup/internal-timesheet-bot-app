using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> GetUserId(this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.GetDataverseUserOrBreak(
            skipFactory: static context => new()
            {
                Skip = context.FlowState.UserId is not null
            },
            failureUserMessage: UnexpectedFailureUserMessage,
            mapFlowState: static (flowState, user) => flowState with
            {
                UserId = user.SystemUserId
            });
}