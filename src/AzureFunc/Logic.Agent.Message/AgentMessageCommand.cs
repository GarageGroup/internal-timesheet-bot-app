using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentMessageResource;

internal sealed class AgentMessageCommand(IAgentMessageApi agentApi)
    : IChatCommand<AgentMessageCommandIn, Unit>, IChatCommandParser<AgentMessageCommandIn>
{
    public Optional<AgentMessageCommandIn> Parse(ChatUpdate update)
    {
        var message = update.Message;
        if (message is null ||
            string.IsNullOrWhiteSpace(message.Text) ||
            message.Entities.AsEnumerable().Any(static entity => entity.Type is BotMessageEntityType.BotCommand))
        {
            return default;
        }

        var userId = message.From?.Id ?? update.User.Id;

        return new AgentMessageCommandIn(
            update.UpdateId,
            userId,
            update.Chat.Id,
            message.Text,
            update.User.LanguageCode);
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        var localizer = request.Context.GetLocalizer(BaseName);

        try
        {
            var input = request.Value;
            var response = await agentApi.SendMessageAsync(
                input.TelegramUpdateId,
                input.TelegramUserId,
                input.TelegramChatId,
                input.Text,
                input.Locale,
                cancellationToken).ConfigureAwait(false);

            _ = await request.Context.Api.SendMessageAsync(
                new(WebUtility.HtmlEncode(response.Text))
                {
                    ParseMode = BotParseMode.Html,
                    ReplyMarkup = response.PreparedAction is null
                        ? new BotReplyKeyboardRemove()
                        : BuildActionKeyboard(response.PreparedAction.ActionId, localizer)
                },
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ProfileNotFound],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.Conflict)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[ConversationConflict],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentMessageCommand>().LogError(exception, "Agent message request failed");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                localizer[UnexpectedError],
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }

    private static BotInlineKeyboardMarkup BuildActionKeyboard(Guid actionId, IStringLocalizer localizer)
        =>
        new()
        {
            InlineKeyboard =
            [
                [
                    new(localizer[ConfirmButton]) { CallbackData = AgentActionCommand.BuildCallbackData(actionId, AgentActionDecision.Confirm) },
                    new(localizer[CancelButton]) { CallbackData = AgentActionCommand.BuildCallbackData(actionId, AgentActionDecision.Cancel) }
                ]
            ]
        };
}
