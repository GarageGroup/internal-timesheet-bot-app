using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> NextValue<TNext>(Func<IChatFlowContext<T>, CancellationToken, ValueTask<TNext>> nextAsync)
        =>
        InnerNextValue(
            nextAsync ?? throw new ArgumentNullException(nameof(nextAsync)));

    private ChatFlow<TNext> InnerNextValue<TNext>(Func<IChatFlowContext<T>, CancellationToken, ValueTask<TNext>> nextAsync)
        =>
        chatFlowEngine.InternalForwardValue(
            async (context, token) => ChatFlowAction.Next(
                await nextAsync.Invoke(context, token).ConfigureAwait(false)))
        .ToChatFlow();
}