using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class TimesheetUserIdGetFlowStep
{
    public static ChatFlow<TFlowState> GetUserId<TFlowState>(
        this ChatFlow<TFlowState> chatFlow, Func<TFlowState, Guid, TFlowState> mapFlowState)
    {
        ArgumentNullException.ThrowIfNull(chatFlow);
        ArgumentNullException.ThrowIfNull(mapFlowState);

        return chatFlow.ForwardValue(GetUserIdJumpAsync, mapFlowState);
    }

    private static ValueTask<ChatFlowJump<Guid>> GetUserIdJumpAsync<TFlowState>(
        IChatFlowContext<TFlowState> context, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            default(Unit), cancellationToken)
        .HandleCancellation()
        .PipeValue(
            context.BotUserProvider.InvokeAsync)
        .MapFailure(
            static _ => Failure.Create("Bot user must be specified"))
        .Forward(
            static user => user.Claims.AsEnumerable().GetValueOrAbsent("DataverseSystemUserId").Fold(ParseOrFailure, CreateClaimMustBeSpecifiedFailure))
        .MapFailure(
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<Guid>);

    private static Result<Guid, Failure<Unit>> ParseOrFailure(string value)
        =>
        Guid.TryParse(value, out var guid) ? guid : Failure.Create($"DataverseUserId Claim {value} is not a Guid");

    private static Result<Guid, Failure<Unit>> CreateClaimMustBeSpecifiedFailure()
        =>
        Failure.Create("Dataverse user claim must be specified");

    private static ChatFlowBreakState ToBreakState(Failure<Unit> failure)
        =>
        ChatFlowBreakState.From(
            userMessage: "Произошла непредвиденная ошибка. Обратитесь к администратору или повторите попытку позднее",
            logMessage: failure.FailureMessage);
}