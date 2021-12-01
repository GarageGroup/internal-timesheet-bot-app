using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public static ChatFlowAction<T> Interrupt(Unit _) => default;
}