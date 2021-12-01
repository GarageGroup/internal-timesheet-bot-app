using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> ForwardValue<TOut, TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TOut>>> forwardAsync,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForwardValue(
            forwardAsync ?? throw new ArgumentNullException(nameof(forwardAsync)),
            mapFlowStateOut ?? throw new ArgumentNullException(nameof(mapFlowStateOut)));

    private ChatFlow<TNext> InnerForwardValue<TOut, TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TOut>>> forwardAsync,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForwardValue(
            async (context, token) =>
            {
                var action = await forwardAsync.Invoke(context, token).ConfigureAwait(false);
                return action.InternalMapFlowState(@out => mapFlowStateOut.Invoke(context.FlowState, @out));
            });
}