using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetShowFlowStep
{
    internal static ChatFlow<TimesheetShowFlowState> GetTimesheetSet(
        this ChatFlow<TimesheetShowFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.SendChatAction(BotChatAction.Typing).ForwardValue(timesheetApi.GetTimesheetSetOrBreakAsync);

    private static ValueTask<ChatFlowJump<TimesheetShowFlowState>> GetTimesheetSetOrBreakAsync(
        this ICrmTimesheetApi timesheetApi, IChatFlowContext<TimesheetShowFlowState> context, CancellationToken cancellationToken)
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
            context.ToBreakState)
        .MapSuccess(
            @out => context.FlowState with
            {
                Timesheets = @out.Timesheets.Map(context.MapTimesheet).ToArray()
            })
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetShowFlowState>);

    private static TimesheetJson MapTimesheet(this IChatFlowContextBase context, TimesheetSetGetItem timesheet)
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
                TypeDisplayName = context.GetDisplayName(timesheet.ProjectType)
            },
            Description = timesheet.Description
        };

    private static ChatBreakState ToBreakState(this IChatFlowContextBase context, Failure<TimesheetSetGetFailureCode> failure)
        =>
        failure.ToChatBreakState(
            userMessage: failure.FailureCode switch
            {
                TimesheetSetGetFailureCode.NotAllowed => context.Localizer[TimesheetShowResource.NotAllowedText],
                _ => throw failure.ToException()
            });
}