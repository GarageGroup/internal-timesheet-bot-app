using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetCreateHelper
{
    internal static ValueTask<ChatFlowJump<Unit>> CreateTimesheetAsync(
        this ITimesheetCreateSupplier timesheetApi, IChatFlowContext<TimesheetCreateFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .HandleCancellation()
        .Pipe(
            flowState => new TimesheetCreateIn(
                date: flowState.Date,
                projectId: flowState.ProjectId,
                projectType: flowState.ProjectType,
                duration: flowState.ValueHours,
                description: flowState.Description,
                channel: context.GetChannel())
            {
                CallerUserId = flowState.UserId
            })
        .PipeValue(
            timesheetApi.CreateTimesheetAsync)
        .Map(
            Unit.From,
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<Unit>);

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