using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

internal static class TimesheetCreateFlowStep
{
    internal static ChatFlow<Unit> CreateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow,
        ITimesheetCreateFunc timesheetCreateFunc)
        =>
        chatFlow.ForwardValue(
            (context, token) => context.CreateTimesheetAsync(timesheetCreateFunc, token))
        .SendText(
            static _ => "Списание времени создано успешно");

    private static ValueTask<ChatFlowJump<Unit>> CreateTimesheetAsync(
        this IChatFlowContext<TimesheetCreateFlowState> context,
        ITimesheetCreateFunc timesheetCreateFunc,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            flowState => new TimesheetCreateIn(
                date: flowState.Date,
                projectId: flowState.ProjectId,
                projectType: flowState.ProjectType,
                duration: flowState.ValueHours,
                description: flowState.Description,
                channel: context.GetChannel()))
        .PipeValue(
            timesheetCreateFunc.InvokeAsync)
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