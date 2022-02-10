using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

public static class TimesheetCreateBotBuilder
{
    public static IBotBuilder UseTimesheetCreate(
        this IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, IFavoriteProjectSetGetFunc> favoriteProjectSetGetFuncResolver,
        Func<IBotContext, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IBotContext, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        InnerUseTimesheetCreate(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandName,
            favoriteProjectSetGetFuncResolver ?? throw new ArgumentNullException(nameof(favoriteProjectSetGetFuncResolver)),
            projectSetSearchFuncResolver ?? throw new ArgumentNullException(nameof(projectSetSearchFuncResolver)),
            timesheetCreateFuncResolver ?? throw new ArgumentNullException(nameof(timesheetCreateFuncResolver)));

    private static IBotBuilder InnerUseTimesheetCreate(
        IBotBuilder botBuilder,
        string commandName,
        Func<IBotContext, IFavoriteProjectSetGetFunc> favoriteProjectSetGetFuncResolver,
        Func<IBotContext, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IBotContext, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        botBuilder.Use(
            (context, cancellationToken) => context.InnerTimesheetCreateAsync(
                commandName,
                context.BotUserProvider,
                favoriteProjectSetGetFuncResolver.Invoke(context),
                projectSetSearchFuncResolver.Invoke(context),
                timesheetCreateFuncResolver.Invoke(context),
                cancellationToken));

    private static ValueTask<Unit> InnerTimesheetCreateAsync(
        this IBotContext botContext,
        string commandName,
        IBotUserProvider botUserProvider,
        IFavoriteProjectSetGetFunc favoriteProjectSetGetFunc,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            commandName, cancellationToken)
        .PipeValue(
            botContext.InternalRecoginzeOrFailureAsync)
        .MapSuccessValue(
            (flow, token) => flow.CreateTimesheet(botUserProvider, favoriteProjectSetGetFunc, projectSetSearchFunc, timesheetCreateFunc).CompleteValueAsync(token))
        .Fold(
            Unit.From,
            Unit.From);
}