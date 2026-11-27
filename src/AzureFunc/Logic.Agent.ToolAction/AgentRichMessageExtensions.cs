using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentRichMessageExtensions
{
    // Sends a rich message (Bot API sendRichMessage) and falls back to regular HTML messages if Telegram rejects it.
    // The fallback is used only when Telegram has definitely refused the rich message. After a timeout or a network error
    // the message may have been delivered, so the error is thrown and the user does not get the same answer twice
    internal static async Task SendRichHtmlOrFallbackAsync(
        this IChatApi api,
        string richHtml,
        string fallbackHtml,
        BotReplyMarkupBase? replyMarkup,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await api.SendRichMessageAsync(
                new(BotInputRichMessage.FromHtml(richHtml))
                {
                    ReplyMarkup = replyMarkup
                },
                cancellationToken).ConfigureAwait(false);

            return;
        }
        catch (Failure<TelegramBotFailureCode>.Exception exception) when (exception.FailureCode is TelegramBotFailureCode.BadRequest)
        {
            logger.LogWarning(exception, "Telegram has rejected a rich message. A regular message will be sent instead");
        }

        var parts = AgentRichHtml.SplitFallback(fallbackHtml);
        for (var index = 0; index < parts.Count; index++)
        {
            _ = await api.SendMessageAsync(
                new(parts[index])
                {
                    ParseMode = BotParseMode.Html,
                    ReplyMarkup = index == parts.Count - 1 ? replyMarkup : null
                },
                cancellationToken).ConfigureAwait(false);
        }
    }
}
