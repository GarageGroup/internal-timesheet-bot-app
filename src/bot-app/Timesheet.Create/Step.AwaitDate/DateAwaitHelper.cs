using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class DateAwaitHelper
{
    internal static TimesheetDateStepOption GetTimesheetDateStepOption(IChatFlowContext<TimesheetCreateFlowState> _)
        =>
        new("Дата списания", 2);
}