using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> CreateTimesheet(
        this ChatFlow chatFlow,
        IBotUserProvider botUserProvider,
        IFavoriteProjectSetGetFunc favoriteProjectSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        chatFlow.Start(
            static () => new TimesheetCreateFlowStateJson())
        .FindProject(
            botUserProvider,
            favoriteProjectSetGetFunc,
            projectSetSearchFunc)
        .GetDate()
        .GetHourValue()
        .GetDescription()
        .ConfirmCreation()
        .CreateTimesheet(
            timesheetCreateFunc);
}