using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

public static class TimesheetSetGetBotBuilder
{
    public static IBotBuilder UseTimesheetSetGet(
        this IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, ITimesheetSetGetFunc> timesheetSetGetFuncResolver)
        =>
        InnerUseTimesheetSetGet(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandName,
            timesheetSetGetFuncResolver ?? throw new ArgumentNullException(nameof(timesheetSetGetFuncResolver)));

    private static IBotBuilder InnerUseTimesheetSetGet(
        IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, ITimesheetSetGetFunc> timesheetSetGetFuncResolver)
        =>
        botBuilder.Use(
            (context, cancellationToken) => context.InnerTimesheetSetGetAsync(
                commandName,
                context.BotUserProvider,
                timesheetSetGetFuncResolver.Invoke(context),
                cancellationToken));

    private static ValueTask<Unit> InnerTimesheetSetGetAsync(
        this IBotContext botContext,
        string commandName,
        IBotUserProvider botUserProvider,
        ITimesheetSetGetFunc timesheetSetGetFuncResolver,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            commandName, cancellationToken)
        .PipeValue(
            botContext.InternalRecoginzeOrFailureAsync)
        .MapSuccessValue(
            (flow, token) => flow.GetTimesheet(botUserProvider, timesheetSetGetFuncResolver).CompleteValueAsync(token))
        .FoldValue(
            (_, token) => botContext.BotFlow.EndAsync(token),
            (_, token) => botContext.BotFlow.NextAsync(token));
}