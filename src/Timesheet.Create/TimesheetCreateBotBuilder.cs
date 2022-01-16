using System;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

public static class TimesheetCreateBotBuilder
{
    public static IBotBuilder UseTimesheetCreate(
        this IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IBotContext, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        InnerUseTimesheetCreate(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandName,
            projectSetSearchFuncResolver ?? throw new ArgumentNullException(nameof(projectSetSearchFuncResolver)),
            timesheetCreateFuncResolver ?? throw new ArgumentNullException(nameof(timesheetCreateFuncResolver)));

    private static IBotBuilder InnerUseTimesheetCreate(
        IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IBotContext, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        botBuilder.Use(
            (context, token) => context.InnerTimesheetCreateAsync(
                commandName,
                projectSetSearchFuncResolver.Invoke(context),
                timesheetCreateFuncResolver.Invoke(context)));

    private static ValueTask<Unit> InnerTimesheetCreateAsync(
        this IBotContext botContext,
        string commandName,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        AsyncPipeline.Pipe(
            commandName)
        .PipeValue(
            botContext.InternalRecoginzeOrFailureAsync)
        .MapSuccessValue(
            (flow, token) => flow.CreateTimesheet(projectSetSearchFunc, timesheetCreateFunc).CompleteValueAsync(token))
        .Fold(
            Unit.From,
            Unit.From);
}