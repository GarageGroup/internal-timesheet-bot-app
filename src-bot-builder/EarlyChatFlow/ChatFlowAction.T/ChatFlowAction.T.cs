using System;

namespace GGroupp.Infra.Bot.Builder;

public readonly partial struct ChatFlowAction<T> : IEquatable<ChatFlowAction<T>>
{
    private readonly T flowState;

    private readonly object? stepState;

    public ChatFlowActionCode Code { get; }

    public ChatFlowAction(T flowState)
    {
        this.flowState = flowState;
        stepState = default;
        Code = ChatFlowActionCode.Next;
    }

    public ChatFlowAction(object? stepState)
    {
        flowState = default!;
        this.stepState = stepState;
        Code = ChatFlowActionCode.AwaitingAndRetry;
    }

    public ChatFlowAction(bool isCanceling)
    {
        flowState = default!;
        stepState = default;
        Code = isCanceling ? ChatFlowActionCode.Canceling : ChatFlowActionCode.Interruption;
    }
}