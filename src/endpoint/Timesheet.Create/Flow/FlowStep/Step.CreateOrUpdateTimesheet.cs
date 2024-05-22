using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> CreateOrUpdateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.SendChatAction(
            BotChatAction.Typing)
        .Forward(
            ValidateFlowState)
        .ForwardValue(
            crmTimesheetApi.CreateOrUpdateTimesheetAsync);

    private static ChatFlowJump<TimesheetCreateFlowState> ValidateFlowState(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        return InnerValidateDate(context.FlowState).Forward(InnerValidateDuration).Fold(ChatFlowJump.Next, MapFailure);

        Result<TimesheetCreateFlowState, ChatRepeatState> InnerValidateDate(TimesheetCreateFlowState state)
            =>
            context.ValidateDateOrRepeat(state.Date.GetValueOrDefault());

        Result<TimesheetCreateFlowState, ChatRepeatState> InnerValidateDuration(TimesheetCreateFlowState state)
            =>
            context.ValidateDurationOrRepeat(state.Duration.GetValueOrDefault()).MapSuccess(_ => state);

        static ChatFlowJump<TimesheetCreateFlowState> MapFailure(ChatRepeatState state)
            =>
            ChatBreakState.From(state.UserMessage, state.LogMessage, state.SourceException);
    }

    private static ValueTask<ChatFlowJump<TimesheetCreateFlowState>> CreateOrUpdateTimesheetAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        context.FlowState.TimesheetId switch
        {
            null => crmTimesheetApi.CreateTimesheetAsync(context, cancellationToken),
            _ => crmTimesheetApi.UpdateTimesheetAsync(context, cancellationToken)
        }; 

    private static ValueTask<ChatFlowJump<TimesheetCreateFlowState>> CreateTimesheetAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            flowState => new TimesheetCreateIn(
                userId: flowState.UserId,
                date: flowState.Date.GetValueOrDefault(),
                project: new(
                    id: flowState.Project?.Id ?? default,
                    type: flowState.Project?.Type ?? default,
                    displayName: flowState.Project?.Name ?? default),
                duration: flowState.Duration.GetValueOrDefault(),
                description: flowState.Description?.Value))
        .PipeValue(
            crmTimesheetApi.CreateAsync)
        .Map(
            _ => context.FlowState,
            context.ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetCreateFlowState>);

    private static ValueTask<ChatFlowJump<TimesheetCreateFlowState>> UpdateTimesheetAsync(
        this ICrmTimesheetApi crmTimesheetApi,  IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            state => new TimesheetUpdateIn(
                timesheetId: state.TimesheetId.GetValueOrDefault(),
                date: state.Date.GetValueOrDefault(),
                project: new(
                    id: state.Project?.Id ?? default,
                    type: state.Project?.Type ?? default,
                    displayName: state.Project?.Name),
                duration: state.Duration.GetValueOrDefault(),
                description: state.Description?.Value))
        .PipeValue(
            crmTimesheetApi.UpdateAsync)
        .Map(
            _ => context.FlowState,
            context.ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetCreateFlowState>);

    private static ChatBreakState ToBreakState(this IChatFlowContextBase context, Failure<TimesheetCreateFailureCode> failure)
        =>
        failure.ToChatBreakState(
            userMessage: failure.FailureCode switch
            {
                TimesheetCreateFailureCode.NotAllowed => context.Localizer[NotAllowedText],
                _ => throw failure.ToException()
            });

    private static ChatBreakState ToBreakState(this IChatFlowContextBase context, Failure<TimesheetUpdateFailureCode> failure)
        =>
        failure.ToChatBreakState(
            userMessage: failure.FailureCode switch
            {
                TimesheetUpdateFailureCode.NotFound => context.Localizer[NotFoundTimesheetText],
                _ => throw failure.ToException()
            });
}