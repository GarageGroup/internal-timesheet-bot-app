using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> CreateOrUpdateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.ForwardValue(
            crmTimesheetApi.CreateOrUpdateTimesheetAsync);

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
                userId: flowState.UserId.GetValueOrDefault(),
                date: flowState.Date.GetValueOrDefault(),
                project: new(
                    id: flowState.Project?.Id ?? default,
                    type: flowState.Project?.Type ?? default,
                    displayName: flowState.Project?.Name ?? default),
                duration: flowState.ValueHours.GetValueOrDefault(),
                description: flowState.Description?.Value,
                channel: context.GetChannel()))
        .PipeValue(
            crmTimesheetApi.CreateAsync)
        .Map(
            _ => context.FlowState,
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetCreateFlowState>);

    private static ValueTask<ChatFlowJump<TimesheetCreateFlowState>> UpdateTimesheetAsync(
        this ICrmTimesheetApi crmTimesheetApi,
        IChatFlowContext<TimesheetCreateFlowState> context,
        CancellationToken cancellationToken)
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
                duration: state.ValueHours.GetValueOrDefault(),
                description: state.Description?.Value))
        .PipeValue(
            crmTimesheetApi.UpdateAsync)
        .Map(
            _ => context.FlowState,
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetCreateFlowState>);

    private static TimesheetChannel GetChannel(this ITurnContext turnContext)
    {
        if (turnContext.IsTelegramChannel())
        {
            return TimesheetChannel.Telegram;
        }

        if (turnContext.IsMsteamsChannel())
        {
            return TimesheetChannel.Teams;
        }

        if (turnContext.IsEmulatorChannel())
        {
            return TimesheetChannel.Emulator;
        }

        if (turnContext.IsWebchatChannel())
        {
            return TimesheetChannel.WebChat;
        }

        return default;
    }

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetCreateFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            TimesheetCreateFailureCode.NotAllowed
                => "Не удалось создать списание времени. Данная операция не разрешена для вашей учетной записи. Обратитесь к администратору",
            _
                => "При создании списания времени произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => ChatFlowBreakState.From(message, failure.FailureMessage, failure.SourceException));

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetUpdateFailureCode> failure)
        =>
        (failure.FailureCode switch
        {
            TimesheetUpdateFailureCode.NotFound
                => "Списание времени не найдено. Возможно оно уже было удалено ранее",
            _
                => "При изменении списания времени произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее"
        })
        .Pipe(
            message => ChatFlowBreakState.From(message, failure.FailureMessage, failure.SourceException));
}