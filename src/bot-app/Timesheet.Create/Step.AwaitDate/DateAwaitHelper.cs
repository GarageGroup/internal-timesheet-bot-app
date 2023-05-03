using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateAwaitHelper
{
    internal static TimesheetDateStepOption GetTimesheetDateStepOption(IChatFlowContext<TimesheetCreateFlowState> _)
        =>
        new("Дата списания", 2);
}