using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetSetGetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetTimesheetSet(
        this ChatFlow<DateTimesheetFlowState> chatFlow, ITimesheetSetGetSupplier timesheetApi)
        =>
        chatFlow.SetTypingStatus().ForwardValue(timesheetApi.GetTimesheetSetOrBreakAsync);
}