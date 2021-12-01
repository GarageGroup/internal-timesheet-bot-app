using System;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<TNext> Forward<TNext>(Func<IChatFlowContext<T>, ChatFlowAction<TNext>> forward)
        =>
        InnerForward(
            forward ?? throw new ArgumentNullException(nameof(forward)));

    private ChatFlow<TNext> InnerForward<TNext>(Func<IChatFlowContext<T>, ChatFlowAction<TNext>> forward)
        =>
        chatFlowEngine.InternalForwardValue(
            (context, _) => context.InternalPipe(forward).InternalPipe(ValueTask.FromResult))
        .ToChatFlow();
}