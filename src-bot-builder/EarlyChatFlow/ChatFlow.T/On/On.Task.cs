using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> On(Func<IChatFlowContext<T>, CancellationToken, Task<Unit>> onAsync)
        =>
        InnerOn(
            onAsync ?? throw new ArgumentNullException(nameof(onAsync)));

    public ChatFlow<T> On(Func<IChatFlowContext<T>, CancellationToken, Task> onAsync)
        =>
        InnerOn(
            onAsync ?? throw new ArgumentNullException(nameof(onAsync)));

    private ChatFlow<T> InnerOn(Func<IChatFlowContext<T>, CancellationToken, Task<Unit>> onAsync)
        =>
        InnerNextValue(
            async (context, token) =>
            {
                _ = await onAsync.Invoke(context, token).ConfigureAwait(false);
                return context.FlowState;
            });

    private ChatFlow<T> InnerOn(Func<IChatFlowContext<T>, CancellationToken, Task> onAsync)
        =>
        InnerNextValue(
            async (context, token) =>
            {
                await onAsync.Invoke(context, token).ConfigureAwait(false);
                return context.FlowState;
            });
}