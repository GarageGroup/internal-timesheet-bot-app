using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static AgentToolActionResource;

// Sends an answer of the in-bot agent: either the model text or the confirmation message of a prepared action
internal static partial class AgentChatReplySender
{
    internal const string CallbackPrefix = "agent-tool:";

    internal static string BuildCallbackData(string actionId, bool isConfirmed)
        =>
        string.Concat(CallbackPrefix, isConfirmed ? "c:" : "x:", actionId);

    internal static async Task SendAsync(IChatContext context, AgentChatReply reply, CancellationToken cancellationToken)
    {
        if (reply.Action is null)
        {
            _ = await context.Api.SendMessageAsync(
                new(SanitizeHtml(reply.Text))
                {
                    ParseMode = BotParseMode.Html,
                    ReplyMarkup = new BotReplyKeyboardRemove()
                },
                cancellationToken).ConfigureAwait(false);

            return;
        }

        var localizer = context.GetLocalizer(BaseName);
        var (richHtml, fallbackHtml) = AgentToolActionPreview.Build(reply.Action, localizer);

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

    // Keeps the tags the agent is allowed to use and encodes everything else
    internal static string SanitizeHtml(string text)
    {
        var builder = new StringBuilder(text.Length);
        var currentIndex = 0;

        foreach (Match match in AllowedTagRegex().Matches(text))
        {
            _ = builder
                .Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text[currentIndex..match.Index])))
                .Append(match.Value);
            currentIndex = match.Index + match.Length;
        }

        return builder
            .Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(text[currentIndex..])))
            .ToString();
    }

    [GeneratedRegex("</?(?:b|i|code)>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AllowedTagRegex();
}
