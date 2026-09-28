using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

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
            message.From?.LanguageCode ?? update.User.LanguageCode);
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentMessageCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
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

            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                WebUtility.HtmlEncode(response.Text),
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                "Профиль не найден. Откройте Mini App и выполните вход.",
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.Conflict)
        {
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                "Диалог уже обрабатывает другое сообщение. Попробуйте ещё раз.",
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentMessageCommand>().LogError(exception, "Agent message request failed");
            _ = await request.Context.Api.SendHtmlModeTextAndRemoveReplyKeyboardAsync(
                "Не удалось получить ответ агента. Попробуйте позже.",
                cancellationToken).ConfigureAwait(false);

            return request.Context.CreateCancelledResult<Unit>();
        }
    }
}
