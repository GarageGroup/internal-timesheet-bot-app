using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> CreateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.ForwardValue(
            crmTimesheetApi.CreateTimesheetAsync);

    private static ValueTask<ChatFlowJump<TimesheetCreateFlowState>> CreateTimesheetAsync(
        this ICrmTimesheetApi crmTimesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            flowState => new TimesheetCreateIn(
                userId: flowState.UserId,
                date: flowState.Date,
                project: new(
                    id: flowState.ProjectId,
                    type: flowState.ProjectType,
                    displayName: flowState.ProjectName),
                duration: flowState.ValueHours,
                description: flowState.Description,
                channel: context.GetChannel()))
        .PipeValue(
            crmTimesheetApi.CreateAsync)
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
            message => ChatFlowBreakState.From(message, failure.FailureMessage));
}