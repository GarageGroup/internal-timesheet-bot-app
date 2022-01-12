using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class BotInfoGetBotBuilder
{
    public static IBotBuilder UseBotInfoGet(this IBotBuilder botBuilder, Func<IBotContext, BotInfoGetOption> optionResolver)
        =>
        InnerUseBotInfoGet(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            optionResolver ?? throw new ArgumentNullException(nameof(optionResolver)));

    private static IBotBuilder InnerUseBotInfoGet(IBotBuilder botBuilder, Func<IBotContext, BotInfoGetOption> optionResolver)
        =>
        botBuilder.Use(
            (context, token) => context.InvokeAsync(optionResolver.Invoke(context), token));

    private static ValueTask<Unit> InvokeAsync(this IBotContext context, BotInfoGetOption option, CancellationToken token)
        =>
        context.TurnContext.Activity.RecognizeCommandOrAbsnet(option.CommandName).FoldValueAsync(
            _ => context.TurnContext.SendBotInfoAsync(option.HelloText, token),
            () => context.BotFlow.NextAsync(token));

    private static async ValueTask<Unit> SendBotInfoAsync(this ITurnContext context, string helloText, CancellationToken token)
    {
        if (string.IsNullOrEmpty(helloText))
        {
            return default;
        }

        var activity = MessageFactory.Text(helloText);
        _ = await context.SendActivityAsync(activity, token).ConfigureAwait(false);

        return default;
    }
}