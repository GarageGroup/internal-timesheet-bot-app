using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

internal static class TimesheetCreateFlowStep
{
    internal static ChatFlow<Unit> CreateTimesheet(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        chatFlow.ForwardValue(
            (context, token) => context.CreateTimesheetAsync(timesheetCreateFunc, token))
        .SendText(
            _ => "Списание времени создано успешно");

    private static ValueTask<ChatFlowJump<Unit>> CreateTimesheetAsync(
        this IChatFlowContext<TimesheetCreateFlowStateJson> context,
        ITimesheetCreateFunc timesheetCreateFunc,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            flowState => new TimesheetCreateIn(
                date: flowState.Date,
                projectId: flowState.ProjectId,
                projectType: flowState.ProjectType,
                duration: flowState.ValueHours,
                description: flowState.Description))
        .PipeValue(
            timesheetCreateFunc.InvokeAsync)
        .Map(
            Unit.From,
            ToUnexpectedBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<Unit>);

    private static ChatFlowBreakState ToUnexpectedBreakState<TFailureCode>(
        Failure<TFailureCode> failure)
        where TFailureCode : struct
        =>
        ChatFlowBreakState.From(
            userMessage: "При создании списания времени произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            logMessage: failure.FailureMessage);
}