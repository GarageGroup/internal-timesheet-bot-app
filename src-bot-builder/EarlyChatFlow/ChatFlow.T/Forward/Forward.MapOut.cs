using System;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> Forward<TOut, TNext>(
        Func<IChatFlowContext<T>, ChatFlowAction<TOut>> forward,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForward(
            forward ?? throw new ArgumentNullException(nameof(forward)),
            mapFlowStateOut ?? throw new ArgumentNullException(nameof(mapFlowStateOut)));

    private ChatFlow<TNext> InnerForward<TOut, TNext>(
        Func<IChatFlowContext<T>, ChatFlowAction<TOut>> forward,
        Func<T, TOut, TNext> mapFlowStateOut)
        =>
        InnerForward(
            context => context.InternalPipe(forward).InternalMapFlowState(
                @out => mapFlowStateOut.Invoke(context.FlowState, @out)));
}