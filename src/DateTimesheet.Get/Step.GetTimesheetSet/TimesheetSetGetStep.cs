using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

internal static class TimesheetSetGetStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetTimesheetSet(
        this ChatFlow<DateTimesheetFlowState> chatFlow, ITimesheetSetGetFunc timesheetSetGetFunc)
        =>
        chatFlow.ForwardValue(timesheetSetGetFunc.GetTimesheetSetJumpAsync);

    private static ValueTask<ChatFlowJump<DateTimesheetFlowState>> GetTimesheetSetJumpAsync(
        this ITimesheetSetGetFunc timesheetSetGetFunc, IChatFlowContext<DateTimesheetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe<TimesheetSetGetIn>(
            static flowState => new(flowState.UserId, flowState.Date))
        .PipeValue(
            timesheetSetGetFunc.InvokeAsync)
        .MapFailure(
            ToBreakState)
        .MapSuccess(
            @out => context.FlowState with
            {
                Timesheets = @out.Timesheets.Select(MapTimesheet).ToArray()
            })
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<DateTimesheetFlowState>);

    private static TimesheetJson MapTimesheet(TimesheetSetItemGetOut timesheet)
        =>
        new()
        {
            Duration = timesheet.Duration,
            ProjectName = timesheet.ProjectName,
            Description = timesheet.Description
        };

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetSetGetFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            TimesheetSetGetFailureCode.NotAllowed
                => "Данная операция не разрешена для вашей учетной записи. Обратитесь к администратору",
            _
                => "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => ChatFlowBreakState.From(message, failure.FailureMessage));
}