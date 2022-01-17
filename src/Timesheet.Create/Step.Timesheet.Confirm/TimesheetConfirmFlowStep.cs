using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowStateJson> ConfirmCreation(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.SendActivity(
            TimesheetConfirmActivity.CreateActivity)
        .Forward(
            TimesheetConfirmActivity.GetConfirmationResult);
}