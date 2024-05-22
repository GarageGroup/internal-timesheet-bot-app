using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ShowDateTimesheets(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.RunCommand(
            CreateTimesheetShowCommandInput);

    private static TimesheetShowCommandIn CreateTimesheetShowCommandInput(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new()
        {
            Date = context.FlowState.Date,
            MessageText = context.FlowState.TimesheetId switch
            {
                null => context.Localizer[CreationSuccessText],
                _ => context.Localizer[UpdateSuccessText]
            }
        };
}