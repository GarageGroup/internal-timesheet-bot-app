using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetDeleteResource;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> ShowDateTimesheets(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow)
        =>
        chatFlow.RunCommand(
            CreateTimesheetShowCommandInput);

    private static TimesheetShowCommandIn CreateTimesheetShowCommandInput(IChatFlowContext<TimesheetDeleteFlowState> context)
        =>
        new()
        {
            Date = context.FlowState.Date,
            MessageText = context.Localizer[DeletionSuccessText]
        };
}