using System;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

public static class BotStartBotBuilder
{
    public static IBotBuilder UseTimesheetCreate(
        this IBotBuilder botBuilder,
        Func<IServiceProvider, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IServiceProvider, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        InnerUseTimesheetCreate(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            projectSetSearchFuncResolver ?? throw new ArgumentNullException(nameof(projectSetSearchFuncResolver)),
            timesheetCreateFuncResolver ?? throw new ArgumentNullException(nameof(timesheetCreateFuncResolver)));

    private static IBotBuilder InnerUseTimesheetCreate(
        IBotBuilder botBuilder,
        Func<IServiceProvider, IProjectSetSearchFunc> projectSetSearchFuncResolver,
        Func<IServiceProvider, ITimesheetCreateFunc> timesheetCreateFuncResolver)
        =>
        botBuilder.Use(
            (context, token) => context.InnerTimesheetCreateAsync(
                projectSetSearchFuncResolver.Invoke(context.ServiceProvider),
                timesheetCreateFuncResolver.Invoke(context.ServiceProvider)));

    private static ValueTask<TurnState> InnerTimesheetCreateAsync(
        this IBotContext botContext,
        IProjectSetSearchFunc projectSetSearchFunc,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        AsyncPipeline.Pipe(
            botContext)
        .PipeValue(
            TimesheetCreateChatFlow.InternalRecoginzeOrFailureAsync)
        .MapSuccessValue(
            (provider, token) => provider.InternalInvokeFlowAsync(
                projectSetSearchFunc: projectSetSearchFunc,
                timesheetCreateFunc: timesheetCreateFunc,
                logger: botContext.LoggerFactory.CreateLogger(nameof(TimesheetCreateChatFlow)),
                cancellationToken: token))
        .Fold(
            Pipeline.Pipe,
            _ => TurnState.Completed);
}