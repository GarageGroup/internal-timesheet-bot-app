using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> CreateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ITimesheetCreateSupplier timesheetApi)
        =>
        chatFlow.ForwardValue(
            timesheetApi.CreateTimesheetAsync,
            static (flowState, _) => flowState);
}