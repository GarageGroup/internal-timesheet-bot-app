using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetGetFlowStep
{
    internal static ChatFlow<TimesheetGetFlowState> GetTimesheetSet(
        this ChatFlow<TimesheetGetFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SetTypingStatus().ForwardValue(timesheetApi.GetTimesheetSetOrBreakAsync);

    private static ValueTask<ChatFlowJump<TimesheetGetFlowState>> GetTimesheetSetOrBreakAsync(
        this ICrmTimesheetApi timesheetApi, IChatFlowContext<TimesheetGetFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            static flowState => new TimesheetSetGetIn(
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
            ChatFlowJump.Break<TimesheetGetFlowState>);

    private static TimesheetJson MapTimesheet(TimesheetSetGetItem timesheet)
        =>
        new()
        {
            Id = timesheet.Id,
            Duration = timesheet.Duration,
            Project = new()
            {
                Id = timesheet.ProjectId,
                Type = timesheet.ProjectType,
                Name = timesheet.ProjectName,
                DisplayTypeName = timesheet.ProjectType.ToDisplayText()
            },            
            Description = timesheet.Description
        };

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetSetGetFailureCode> failure)
        =>
        failure.SourceException.ToChatFlowBreakState(
            userMessage: failure.FailureCode switch
            {
                TimesheetSetGetFailureCode.NotAllowed => NotAllowedFailureUserMessage,
                _ => UnexpectedFailureUserMessage
            },
            logMessage: failure.FailureMessage);
}