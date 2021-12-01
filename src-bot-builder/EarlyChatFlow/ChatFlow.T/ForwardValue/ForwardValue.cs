using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> ForwardValue<TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TNext>>> forwardAsync)
        =>
        InnerForwardValue(
            forwardAsync ?? throw new ArgumentNullException(nameof(forwardAsync)));

    private ChatFlow<TNext> InnerForwardValue<TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TNext>>> forwardAsync)
        =>
        chatFlowEngine.InternalForwardValue(forwardAsync).ToChatFlow();
}