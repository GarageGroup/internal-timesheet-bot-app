using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class BotConversationCancelBotBuilder
{
    public static IBotBuilder UseConversationCancel(this IBotBuilder botBuilder, Func<IBotContext, BotConversationCancelOption> optionResolver)
        =>
        InnerUseConversationCancel(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            optionResolver ?? throw new ArgumentNullException(nameof(optionResolver)));

    private static IBotBuilder InnerUseConversationCancel(IBotBuilder botBuilder, Func<IBotContext, BotConversationCancelOption> optionResolver)
        =>
        botBuilder.Use(
            (context, token) => context.InvokeAsync(optionResolver.Invoke(context), token));

    private static ValueTask<Unit> InvokeAsync(this IBotContext context, BotConversationCancelOption option, CancellationToken token)
        =>
        context.TurnContext.Activity.RecognizeCommandOrAbsnet(option.CommandName).FoldValueAsync(
            _ => context.CancelCommandAsync(option.SuccessText, token),
            () => context.BotFlow.NextAsync(token));

    private static async ValueTask<Unit> CancelCommandAsync(this IBotContext context, string successText, CancellationToken token)
    {
        var user = await context.BotUserProvider.GetCurrentUserAsync(token).ConfigureAwait(false);

        await context.UserState.ClearStateAsync(context.TurnContext, token).ConfigureAwait(false);
        await context.ConversationState.ClearStateAsync(context.TurnContext, token).ConfigureAwait(false);

        if (user is not null)
        {
            _ = await context.BotUserProvider.SetCurrentUserAsync(user, token).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(successText) is false)
        {
            var activity = MessageFactory.Text(successText);
            _ = await context.TurnContext.SendActivityAsync(activity, token).ConfigureAwait(false);
        }

        return default;
    }
}