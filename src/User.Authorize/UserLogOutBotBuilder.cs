using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;

namespace GGroupp.Internal.Timesheet;

public static class UserLogOutBotBuilder
{
    public static IBotBuilder UseUserLogOut(this IBotBuilder botBuilder, Func<IBotContext, UserLogOutOption> commandResolver)
        =>
        InnerUseUserLogOut(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            commandResolver ?? throw new ArgumentNullException(nameof(commandResolver)));

    private static IBotBuilder InnerUseUserLogOut(IBotBuilder botBuilder, Func<IBotContext, UserLogOutOption> commandResolver)
        =>
        botBuilder.Use(
            (context, token) => InvokeAsync(context, commandResolver.Invoke(context), token));

    private static ValueTask<Unit> InvokeAsync(IBotContext context, UserLogOutOption option, CancellationToken token)
    {
        if (context.TurnContext.Activity.ChannelId is Channels.Msteams)
        {
            return context.BotFlow.NextAsync(token);
        }

        return context.TurnContext.Activity.RecognizeCommandOrAbsnet(option.CommandName).FoldValueAsync(
            _ => context.LogoutAsync(token),
            () => context.BotFlow.NextAsync(token));
    }

    private static async ValueTask<Unit> LogoutAsync(this IBotContext context, CancellationToken token)
    {
        var user = await context.BotUserProvider.GetCurrentUserAsync(token).ConfigureAwait(false);
        if (user is null)
        {
            var activity = MessageFactory.Text("Вы не авторизованы");
            _ = await context.TurnContext.SendActivityAsync(activity, token).ConfigureAwait(false);

            return default;
        }

        _ = await context.BotUserProvider.SetCurrentUserAsync(default, token).ConfigureAwait(false);

        await context.UserState.ClearStateAsync(context.TurnContext, token).ConfigureAwait(false);
        await context.ConversationState.ClearStateAsync(context.TurnContext, token).ConfigureAwait(false);

        var successActivity = MessageFactory.Text("Вы вышли из учетной записи");
        _ = await context.TurnContext.SendActivityAsync(successActivity, token).ConfigureAwait(false);

        return default;
    }
}