using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

public static class DateTimesheetGetBotBuilder
{
    public static IBotBuilder UseDateTimesheetGet(
        this IBotBuilder botBuilder, string commandName, Func<IBotContext, ITimesheetSetGetFunc> timesheetSetGetFuncResolver)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = timesheetSetGetFuncResolver ?? throw new ArgumentNullException(nameof(timesheetSetGetFuncResolver));

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext botContext, CancellationToken cancellationToken)
            =>
            botContext.InnerDateTimesheetGetAsync(
                commandName ?? string.Empty,
                timesheetSetGetFuncResolver.Invoke(botContext),
                cancellationToken);
    }

    private static ValueTask<Unit> InnerDateTimesheetGetAsync(
        this IBotContext botContext,
        string commandName,
        ITimesheetSetGetFunc timesheetSetGetFuncResolver,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            commandName, cancellationToken)
        .PipeValue(
            botContext.InternalRecoginzeOrFailureAsync)
        .MapSuccessValue(
            (flow, token) => flow.GetTimesheet(timesheetSetGetFuncResolver).CompleteValueAsync(token))
        .FoldValue(
            (_, token) => botContext.BotFlow.EndAsync(token),
            (_, token) => botContext.BotFlow.NextAsync(token));
}