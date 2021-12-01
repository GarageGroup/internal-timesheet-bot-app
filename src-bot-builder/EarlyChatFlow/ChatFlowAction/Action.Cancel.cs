using System;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowAction
{
    public static ChatFlowAction<T> Cancel<T>(Unit _) => new(isCanceling: true);
}