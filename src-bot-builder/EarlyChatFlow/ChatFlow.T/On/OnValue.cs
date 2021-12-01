using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> OnValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask<Unit>> onAsync)
        =>
        InnerOnValue(
            onAsync ?? throw new ArgumentNullException(nameof(onAsync)));

    public ChatFlow<T> OnValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask> onAsync)
        =>
        InnerOnValue(
            onAsync ?? throw new ArgumentNullException(nameof(onAsync)));

    private ChatFlow<T> InnerOnValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask<Unit>> onAsync)
        =>
        InnerNextValue(
            async (context, token) =>
            {
                _ = await onAsync.Invoke(context, token).ConfigureAwait(false);
                return context.FlowState;
            });

    private ChatFlow<T> InnerOnValue(Func<IChatFlowContext<T>, CancellationToken, ValueTask> onAsync)
        =>
        InnerNextValue(
            async (context, token) =>
            {
                await onAsync.Invoke(context, token).ConfigureAwait(false);
                return context.FlowState;
            });
}