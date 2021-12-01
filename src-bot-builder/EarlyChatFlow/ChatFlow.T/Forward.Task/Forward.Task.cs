using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> Forward<TNext>(Func<IChatFlowContext<T>, CancellationToken, Task<ChatFlowAction<TNext>>> forwardAsync)
        =>
        InnerForward(
            forwardAsync ?? throw new ArgumentNullException(nameof(forwardAsync)));

    private ChatFlow<TNext> InnerForward<TNext>(Func<IChatFlowContext<T>, CancellationToken, Task<ChatFlowAction<TNext>>> forwardAsync)
        =>
        chatFlowEngine.InternalForwardValue(
            async (context, token) => await forwardAsync.Invoke(context, token).ConfigureAwait(false))
        .ToChatFlow();
}