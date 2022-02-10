using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowStateJson> FindProject(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        IBotUserProvider botUserProvider,
        IFavoriteProjectSetGetFunc favoriteProjectSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc)
        =>
        chatFlow.AwaitLookupValue(
            (context, token) => context.ShowFavorieProjects(botUserProvider, favoriteProjectSetGetFunc, token),
            (_, search, token) => projectSetSearchFunc.SearchProjectsAsync(search, token),
            static (flowState, projectValue) => flowState with
            {
                ProjectType = Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty()),
                ProjectId = projectValue.Id,
                ProjectName = projectValue.Name
            });
}