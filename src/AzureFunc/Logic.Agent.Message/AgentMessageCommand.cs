using System;
using System.Globalization;
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

            var preparedAction = GetPreparedAction(response);

            _ = await request.Context.Api.SendMessageAsync(
                new(BuildMessageText(response, localizer))
                {
                    ParseMode = BotParseMode.Html,
                    ReplyMarkup = preparedAction is null
                        ? new BotReplyKeyboardRemove()
                        : BuildActionKeyboard(preparedAction.Value.ActionId, preparedAction.Value.ActionType, localizer)
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

    private static (Guid ActionId, AgentActionType ActionType)? GetPreparedAction(AgentMessage message)
    {
        var actionCount = new object?[]
        {
            message.PreparedCreateAction,
            message.PreparedDeleteAction,
            message.PreparedUpdateAction
        }.Count(static action => action is not null);

        if (actionCount > 1)
        {
            throw new InvalidOperationException("Agent response contains more than one prepared action");
        }

        if (message.PreparedCreateAction is not null)
        {
            return (message.PreparedCreateAction.ActionId, AgentActionType.Create);
        }

        if (message.PreparedDeleteAction is not null)
        {
            return (message.PreparedDeleteAction.ActionId, AgentActionType.Delete);
        }

        if (message.PreparedUpdateAction is not null)
        {
            return (message.PreparedUpdateAction.ActionId, AgentActionType.Update);
        }

        return null;
    }

    private static string BuildMessageText(AgentMessage message, IStringLocalizer localizer)
    {
        if (message.PreparedCreateAction is AgentPreparedCreateAction createAction)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                localizer[CreatePreview],
                createAction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                WebUtility.HtmlEncode(createAction.ProjectName),
                createAction.Duration.ToString("0.##", CultureInfo.CurrentCulture),
                WebUtility.HtmlEncode(createAction.Description));
        }

        if (message.PreparedDeleteAction is AgentPreparedDeleteAction deleteAction)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                localizer[DeletePreview],
                deleteAction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                WebUtility.HtmlEncode(deleteAction.ProjectName),
                deleteAction.Duration.ToString("0.##", CultureInfo.CurrentCulture),
                WebUtility.HtmlEncode(deleteAction.Description));
        }

        if (message.PreparedUpdateAction is AgentPreparedUpdateAction updateAction)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                localizer[UpdatePreview],
                updateAction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                WebUtility.HtmlEncode(updateAction.ProjectName),
                updateAction.Duration.ToString("0.##", CultureInfo.CurrentCulture),
                WebUtility.HtmlEncode(updateAction.Description));
        }

        return WebUtility.HtmlEncode(message.Text);
    }

    private static BotInlineKeyboardMarkup BuildActionKeyboard(
        Guid actionId,
        AgentActionType actionType,
        IStringLocalizer localizer)
        =>
        new()
        {
            InlineKeyboard =
            [
                [
                    new(localizer[ConfirmButton])
                    {
                        CallbackData = AgentActionCommand.BuildCallbackData(actionId, actionType, AgentActionDecision.Confirm)
                    },
                    new(localizer[CancelButton])
                    {
                        CallbackData = AgentActionCommand.BuildCallbackData(actionId, actionType, AgentActionDecision.Cancel)
                    }
                ]
            ]
        };
}
