using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

public static class TimesheetCreateBotBuilder
{
    public static IBotBuilder UseTimesheetCreate(
        this IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, IFavoriteSetGetFunc> favoriteSetGetFuncResolver,
        Func<IBotContext, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IBotContext, ITimesheetCreateFunc> timesheetCreateFuncResolver)
    {
        _ = botBuilder ?? throw new ArgumentNullException(nameof(botBuilder));
        _ = favoriteSetGetFuncResolver ?? throw new ArgumentNullException(nameof(favoriteSetGetFuncResolver));
        _ = projectSetSearchFuncResolver ?? throw new ArgumentNullException(nameof(projectSetSearchFuncResolver));
        _ = timesheetCreateFuncResolver ?? throw new ArgumentNullException(nameof(timesheetCreateFuncResolver));

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext context, CancellationToken cancellationToken)
            =>
            context.InnerTimesheetCreateAsync(
                commandName,
                favoriteSetGetFuncResolver.Invoke(context),
                projectSetSearchFuncResolver.Invoke(context),
                timesheetCreateFuncResolver.Invoke(context),
                cancellationToken);
    }

    private static ValueTask<Unit> InnerTimesheetCreateAsync(
        this IBotContext botContext,
        string commandName,
        IFavoriteSetGetFunc favoriteSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            commandName, cancellationToken)
        .HandleCancellation()
        .PipeValue(
            botContext.InternalRecoginzeFlowAsync)
        .MapSuccessValue(
            (flow, token) => flow.Start(favoriteSetGetFunc, projectSetSearchFunc, timesheetCreateFunc).CompleteValueAsync(token))
        .FoldValue(
            botContext.EndFlowAsync,
            botContext.NextFlowAsync);

    private static ValueTask<Unit> EndFlowAsync(this IBotContext context, Unit _, CancellationToken cancellationToken)
        =>
        context.BotFlow.EndAsync(cancellationToken);

    private static ValueTask<Unit> NextFlowAsync(this IBotContext context, Unit _, CancellationToken cancellationToken)
        =>
        context.BotFlow.NextAsync(cancellationToken);
}