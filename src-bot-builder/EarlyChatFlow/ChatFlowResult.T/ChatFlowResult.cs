using System;

namespace GGroupp.Infra.Bot.Builder;

internal readonly partial struct ChatFlowResult<T> : IEquatable<ChatFlowResult<T>>
{
    private readonly T flowState;

    public ChatFlowResultCode Code { get; }

    private ChatFlowResult(T flowState)
    {
        Code = ChatFlowResultCode.Running;
        this.flowState = flowState;
    }

    private ChatFlowResult(ChatFlowResultCode code)
    {
        Code = code;
        flowState = default!;
    }
}