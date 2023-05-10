using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class DateAwaitHelper
{
    internal static TimesheetDateStepOption GetTimesheetDateStepOption(IChatFlowContext<DateTimesheetFlowState> context)
        =>
        new("Дата", 3)
        {
            SkipStep = context.FlowState.Date is not null
        };
}