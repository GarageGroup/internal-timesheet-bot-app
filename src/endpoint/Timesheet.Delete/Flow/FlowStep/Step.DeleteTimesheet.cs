using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> DeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SendChatAction(
            BotChatAction.Typing)
        .ForwardValue(
            timesheetApi.DeleteTimesheetsAsync);

    private static ValueTask<ChatFlowJump<TimesheetDeleteFlowState>> DeleteTimesheetsAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetDeleteFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context, cancellationToken)
        .Pipe(
            static context => new TimesheetDeleteIn(context.FlowState.TimesheetId))
        .PipeValue(
            crmTimesheetApi.DeleteAsync)
        .Recover(
            static failure => failure.FailureCode switch
            {
                TimesheetDeleteFailureCode.NotFound => Result.Success<Unit>(default).With<Failure<Unit>>(),
                _ => failure.WithFailureCode<Unit>(default)
            })
        .SuccessOrThrow(
            static failure => failure.ToException())
        .Pipe(
            _ => ChatFlowJump.Next(context.FlowState));
}