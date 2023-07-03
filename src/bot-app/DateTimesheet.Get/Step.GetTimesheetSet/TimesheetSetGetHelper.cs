using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetSetGetHelper
{
    internal static ValueTask<ChatFlowJump<DateTimesheetFlowState>> GetTimesheetSetOrBreakAsync(
        this ITimesheetSetGetSupplier timesheetApi, IChatFlowContext<DateTimesheetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe<TimesheetSetGetIn>(
            static flowState => new(flowState.UserId, flowState.Date.GetValueOrDefault())
            {
                CallerUserId = flowState.UserId
            })
        .PipeValue(
            timesheetApi.GetTimesheetSetAsync)
        .MapFailure(
            ToBreakState)
        .MapSuccess(
            @out => context.FlowState with
            {
                Timesheets = @out.Timesheets.Map(MapTimesheet).ToArray()
            })
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<DateTimesheetFlowState>);

    private static TimesheetJson MapTimesheet(TimesheetSetItem timesheet)
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