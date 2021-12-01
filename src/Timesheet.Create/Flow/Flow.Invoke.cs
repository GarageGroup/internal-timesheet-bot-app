using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class TimesheetCreateChatFlow
{
    internal static ValueTask<TurnState> InternalInvokeFlowAsync(
        this ChatFlow chatFlow,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc,
        ILogger logger,
        CancellationToken cancellationToken)
        =>
        chatFlow.Start(
            () => new TimesheetCreateFlowStateJson())
        .FindProject(
            projectSetSearchFunc, logger)
        .GetDate()
        .GetHourValue()
        .GetDescription()
        .ConfirmCreation()
        .CreateTimesheet(
            timesheetCreateFunc, logger)
        .CompleteValueAsync(
            cancellationToken);
}