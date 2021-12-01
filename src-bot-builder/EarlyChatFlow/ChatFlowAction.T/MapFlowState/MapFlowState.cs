using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public ChatFlowAction<TResult> MapFlowState<TResult>(Func<T, TResult> mapFlowState)
        =>
        InternalMapFlowState(
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    internal ChatFlowAction<TResult> InternalMapFlowState<TResult>(Func<T, TResult> mapFlowState)
        =>
        Code switch
        {
            ChatFlowActionCode.Next => new(flowState.InternalPipe(mapFlowState)),
            ChatFlowActionCode.AwaitingAndRetry => new(stepState),
            ChatFlowActionCode.Canceling => new(isCanceling: true),
            _ => default
        };
}