using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class DateTimesheetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetTimesheetSet(
        this ChatFlow<DateTimesheetFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SetTypingStatus().ForwardValue(timesheetApi.GetTimesheetSetOrBreakAsync);

    private static ValueTask<ChatFlowJump<DateTimesheetFlowState>> GetTimesheetSetOrBreakAsync(
        this ICrmTimesheetApi timesheetApi, IChatFlowContext<DateTimesheetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe<TimesheetSetGetIn>(
            static flowState => new(
                userId: flowState.UserId,
                date: flowState.Date.GetValueOrDefault()))
        .PipeValue(
            timesheetApi.GetAsync)
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

    private static TimesheetJson MapTimesheet(TimesheetSetGetItem timesheet)
        =>
        new()
        {
            Duration = timesheet.Duration,
            ProjectType = timesheet.ProjectType,
            ProjectName = timesheet.ProjectName,
            Description = timesheet.Description,
            Id = timesheet.Id
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
            message => ChatFlowBreakState.From(message, failure.FailureMessage, failure.SourceException));
}