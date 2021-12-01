using System;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ChatFlow<T> SendActivity(Func<IChatFlowContext<T>, IActivity> activityFactory)
        =>
        InnerSendActivity(
            activityFactory ?? throw new ArgumentNullException(nameof(activityFactory)));

    private ChatFlow<T> InnerSendActivity(Func<IChatFlowContext<T>, IActivity> activityFactory)
        =>
        InnerOn(
            (context, token) => context.SendActivityAsync(activityFactory.Invoke(context), token));
}