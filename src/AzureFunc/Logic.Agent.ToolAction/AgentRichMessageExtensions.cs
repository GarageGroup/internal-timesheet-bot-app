using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal static class AgentRichMessageExtensions
{
    // Sends a rich message (Bot API sendRichMessage) and falls back to a regular HTML message if Telegram rejects it
    internal static async Task<BotMessage> SendRichHtmlOrFallbackAsync(
        this IChatApi api,
        string richHtml,
        string fallbackHtml,
        BotReplyMarkupBase? replyMarkup,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await api.SendRichMessageAsync(
                new(BotInputRichMessage.FromHtml(richHtml))
                {
                    ReplyMarkup = replyMarkup
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to send a rich message. A regular message will be sent instead");
        }

        return await api.SendMessageAsync(
            new(fallbackHtml)
            {
                ParseMode = BotParseMode.Html,
                ReplyMarkup = replyMarkup
            },
            cancellationToken).ConfigureAwait(false);
    }
}
