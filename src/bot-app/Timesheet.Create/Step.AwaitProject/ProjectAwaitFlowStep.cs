using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class ProjectAwaitFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitProject<TTimesheetApi>(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, TTimesheetApi timesheetApi)
        where TTimesheetApi : IFavoriteProjectSetGetSupplier, IProjectSetSearchSupplier
        =>
        chatFlow.AwaitLookupValue(
            timesheetApi.GetFavoriteProjectsAsync,
            timesheetApi.SearchProjectsAsync,
            ProjectAwaitHelper.CreateResultMessage,
            ProjectAwaitHelper.WithProjectValue);
}