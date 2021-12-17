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
            timesheetCreateFunc.CreateTimesheetAsync)
        .SendText(
            _ => "Списание времени создано успешно");

    private static ValueTask<ChatFlowJump<Unit>> CreateTimesheetAsync(
        this ITimesheetCreateFunc timesheetCreateFunc,
        IChatFlowContext<TimesheetCreateFlowStateJson> context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            state => new TimesheetCreateIn(
                ownerId: default,
                date: state.Date,
                projectId: state.ProjectId,
                projectType: state.ProjectType,
                duration: state.ValueHours,
                description: state.Description))
        .PipeValue(
            timesheetCreateFunc.InvokeAsync)
        .Map(
            Unit.From,
            failure => ChatFlowBreakState.From(
                uiMessage: "При создании списания времени произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
                logMessage: failure.FailureMessage))
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<Unit>);
}