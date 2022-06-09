using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> Start(
        this ChatFlow chatFlow,
        IFavoriteSetGetFunc favoriteSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        chatFlow.Start<TimesheetCreateFlowState>(
            static () => new())
        .GetUserId()
        .FindProject(
            favoriteSetGetFunc, projectSetSearchFunc)
        .GetDate()
        .GetHourValue()
        .GetDescription()
        .ConfirmTimesheet()
        .CreateTimesheet(
            timesheetCreateFunc);
}