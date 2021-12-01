using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> Forward<TIn, TOut, TNext>(
        Func<T, TIn> mapFlowStateIn,
        Func<IChatFlowContext<TIn>, CancellationToken, Task<ChatFlowAction<TOut>>> forwardAsync,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForward(
            mapFlowStateIn ?? throw new ArgumentNullException(nameof(mapFlowStateIn)),
            forwardAsync ?? throw new ArgumentNullException(nameof(forwardAsync)),
            mapFlowStateOut ?? throw new ArgumentNullException(nameof(mapFlowStateOut)));

    private ChatFlow<TNext> InnerForward<TIn, TOut, TNext>(
        Func<T, TIn> mapFlowStateIn,
        Func<IChatFlowContext<TIn>, CancellationToken, Task<ChatFlowAction<TOut>>> forwardAsync,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForwardValue(
            async (context, token) =>
            {
                var mappedContext = context.InternalMapFlowState(mapFlowStateIn);
                var action = await forwardAsync.Invoke(mappedContext, token).ConfigureAwait(false);
                return action.InternalMapFlowState(@out => mapFlowStateOut.Invoke(context.FlowState, @out));
            });
}