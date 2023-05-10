using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetConfirmFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ConfirmTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(
            TimesheetConfirmHelper.CreateOption)
        .SetTypingStatus();
}