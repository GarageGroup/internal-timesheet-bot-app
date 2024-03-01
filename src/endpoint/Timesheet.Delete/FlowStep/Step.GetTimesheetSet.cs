using GarageGroup.Infra.Bot.Builder;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> GetTimesheetSet(
        this ChatFlow<DeleteTimesheetFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SetTypingStatus().ForwardValue(timesheetApi.GetTimesheetSetOrBreakAsync);

    private static ValueTask<ChatFlowJump<DeleteTimesheetFlowState>> GetTimesheetSetOrBreakAsync(
        this ICrmTimesheetApi timesheetApi, IChatFlowContext<DeleteTimesheetFlowState> context, CancellationToken cancellationToken)
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
            SuccessGetTimesheetAsync,
            ChatFlowJump.Break<DeleteTimesheetFlowState>);

    private static ChatFlowJump<DeleteTimesheetFlowState> SuccessGetTimesheetAsync(DeleteTimesheetFlowState state)
    {
        if (state.Timesheets?.Count == 0)
        {
            var textBuilder = new StringBuilder($"Нет списаний времени за {state.Date}")
                .Append(TelegramBotLine)
                .Append(LineSeparator)
                .Append(TelegramBotLine)
                .Append("/newtimesheet - Списать время");

            return ChatFlowJump.Break<DeleteTimesheetFlowState>(ChatFlowBreakState.From(textBuilder.ToString()));
        }

        return ChatFlowJump.Next(state);
    }

    private static TimesheetJson MapTimesheet(TimesheetSetGetItem timesheet)
        =>
        new()
        {
            Duration = timesheet.Duration,
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