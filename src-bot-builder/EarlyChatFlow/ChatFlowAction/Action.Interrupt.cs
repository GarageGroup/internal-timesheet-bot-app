using System;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowAction
{
    public static ChatFlowAction<T> Interrupt<T>(Unit _) => default;
}