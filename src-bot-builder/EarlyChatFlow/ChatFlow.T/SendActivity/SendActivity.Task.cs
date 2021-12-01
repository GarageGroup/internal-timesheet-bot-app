using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> SendActivity(Func<IChatFlowContext<T>, CancellationToken, Task<IActivity>> activityFactoryAsync)
        =>
        InnerSendActivity(
            activityFactoryAsync ?? throw new ArgumentNullException(nameof(activityFactoryAsync)));

    private ChatFlow<T> InnerSendActivity(Func<IChatFlowContext<T>, CancellationToken, Task<IActivity>> activityFactoryAsync)
        =>
        InnerOnValue(
            async (context, token) =>
            {
                var activity = await activityFactoryAsync.Invoke(context, token).ConfigureAwait(false);
                _ = await context.SendActivityAsync(activity, token).ConfigureAwait(false);
            });
}