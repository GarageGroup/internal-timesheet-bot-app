using System;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> On(Func<IChatFlowContext<T>, Unit> on)
        =>
        InnerOn(
            on ?? throw new ArgumentNullException(nameof(on)));

    public ChatFlow<T> On(Action<IChatFlowContext<T>> on)
        =>
        InnerOn(
            on ?? throw new ArgumentNullException(nameof(on)));

    private ChatFlow<T> InnerOn(Func<IChatFlowContext<T>, Unit> on)
        =>
        InnerNext(
            context =>
            {
                _ = on.Invoke(context);
                return context.FlowState;
            });

    private ChatFlow<T> InnerOn(Action<IChatFlowContext<T>> on)
        =>
        InnerNext(
            context =>
            {
                on.Invoke(context);
                return context.FlowState;
            });
}