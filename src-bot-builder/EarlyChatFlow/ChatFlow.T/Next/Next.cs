using System;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> Next<TNext>(Func<IChatFlowContext<T>, TNext> nextAsync)
        =>
        InnerNext(
            nextAsync ?? throw new ArgumentNullException(nameof(nextAsync)));

    private ChatFlow<TNext> InnerNext<TNext>(Func<IChatFlowContext<T>, TNext> next)
        =>
        InnerForward(
            context => context.InternalPipe(next).InternalPipe(ChatFlowAction.Next));
}