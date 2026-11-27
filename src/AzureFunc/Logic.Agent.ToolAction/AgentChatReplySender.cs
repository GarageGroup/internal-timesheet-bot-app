using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static AgentToolActionResource;

// Sends an answer of the in-bot agent: either the model text or the confirmation message of a prepared action
internal static class AgentChatReplySender
{
    internal const string CallbackPrefix = "agent-tool:";

    internal static string BuildCallbackData(string actionId, bool isConfirmed)
        =>
        string.Concat(CallbackPrefix, isConfirmed ? "c:" : "x:", actionId);

    internal static async Task SendAsync(IChatContext context, AgentChatReply reply, string locale, CancellationToken cancellationToken)
    {
        if (reply.Action is null)
        {
            var answer = AgentRichHtml.Prepare(reply.Text);
            if (answer.IsRich)
            {
                _ = await context.Api.SendRichHtmlOrFallbackAsync(
                    answer.Html,
                    AgentRichHtml.ToFallback(answer.Html),
                    new BotReplyKeyboardRemove(),
                    context.GetLogger<AgentChatReply>(),
                    cancellationToken).ConfigureAwait(false);

                return;
            }

            _ = await context.Api.SendMessageAsync(
                new(answer.Html)
                {
                    ParseMode = BotParseMode.Html,
                    ReplyMarkup = new BotReplyKeyboardRemove()
                },
                cancellationToken).ConfigureAwait(false);

            return;
        }

        var localizer = context.GetLocalizer(BaseName);
        var (richHtml, fallbackHtml) = AgentToolActionPreview.Build(reply.Action, localizer, locale);

        _ = await context.Api.SendRichHtmlOrFallbackAsync(
            richHtml,
            fallbackHtml,
            new BotInlineKeyboardMarkup
            {
                InlineKeyboard =
                [
                    [
                        new(localizer[ConfirmButton])
                        {
                            CallbackData = BuildCallbackData(reply.Action.Id, isConfirmed: true)
                        },
                        new(localizer[CancelButton])
                        {
                            CallbackData = BuildCallbackData(reply.Action.Id, isConfirmed: false)
                        }
                    ]
                ]
            },
            context.GetLogger<AgentChatReply>(),
            cancellationToken).ConfigureAwait(false);
    }
}
