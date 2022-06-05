using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;

internal static class ProjectFindFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> FindProject(
        this ChatFlow<TimesheetCreateFlowState> chatFlow,
        IBotUserProvider botUserProvider,
        IFavoriteProjectSetGetFunc favoriteProjectSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc)
        =>
        chatFlow.AwaitLookupValue(
            (context, token) => context.ShowFavorieProjects(botUserProvider, favoriteProjectSetGetFunc, token),
            (_, search, token) => projectSetSearchFunc.SearchProjectsAsync(search, token),
            CreateResultMessage,
            static (flowState, projectValue) => flowState with
            {
                ProjectType = projectValue.GetProjectType(),
                ProjectId = projectValue.Id,
                ProjectName = projectValue.Name
            });

    private static string CreateResultMessage(IChatFlowContext<TimesheetCreateFlowState> context, LookupValue projectValue)
        =>
        $"{projectValue.GetProjectType().ToStringRussianCulture()}: {context.EncodeTextWithStyle(projectValue.Name, BotTextStyle.Bold)}";

    private static TimesheetProjectType GetProjectType(this LookupValue projectValue)
        =>
        Enum.Parse<TimesheetProjectType>(projectValue.Data.OrEmpty());
}