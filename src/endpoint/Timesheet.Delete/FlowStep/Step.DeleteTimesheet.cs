using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> DeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SetTypingStatus()
        .ForwardValue(
            timesheetApi.DeleteTimesheetsAsync);

    private static ValueTask<ChatFlowJump<TimesheetDeleteFlowState>> DeleteTimesheetsAsync(
        this ICrmTimesheetApi crmTimesheetApi,
        IChatFlowContext<TimesheetDeleteFlowState> context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .On(
            (context, token) => context.DeleteActivityAsync(context.Activity.Id, cancellationToken))
        .Pipe(
            static context => new TimesheetDeleteIn(context.FlowState.Timesheet?.Id ?? default))
        .PipeValue(
            crmTimesheetApi.DeleteAsync)
        .Recover(
            static failure => failure.FailureCode switch
            {
                TimesheetDeleteFailureCode.NotFound => Result.Success<Unit>(default).With<Failure<Unit>>(),
                _ => failure.WithFailureCode<Unit>(default)
            })
        .Map(
            _ => context.FlowState,
            static failure => failure.SourceException.ToChatFlowBreakState("Не удалось удалить запись", failure.FailureMessage))
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetDeleteFlowState>);
}