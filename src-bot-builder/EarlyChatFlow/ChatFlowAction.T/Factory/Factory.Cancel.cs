using System;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public static ChatFlowAction<T> Cancel(Unit _) => new(isCanceling: true);
}