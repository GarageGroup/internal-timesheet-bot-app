using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> CreateTimesheet(
        this ChatFlow chatFlow,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        chatFlow.Start(
            static () => new TimesheetCreateFlowStateJson())
        .FindProject(
            projectSetSearchFunc)
        .GetDate()
        .GetHourValue()
        .GetDescription()
        .ConfirmCreation()
        .CreateTimesheet(
            timesheetCreateFunc);
}