using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

using static AgentToolActionResource;

// Handles the Confirm and Cancel buttons of an action prepared by the in-bot agent
internal sealed class AgentToolActionCommand(AgentChatFeature chatFeature)
    : IChatCommand<AgentToolActionCommandIn, Unit>, IChatCommandParser<AgentToolActionCommandIn>
{
    public Optional<AgentToolActionCommandIn> Parse(ChatUpdate update)
    {
        if (update.WidgetQuery is BotWidgetQuery query)
        {
            if (query.MessageId is not int widgetMessageId ||
                AgentActionWidget.TryParseQuery(query.Data, out var actionId, out var isConfirmed, out var edit) is false)
            {
                return default;
            }

            return new AgentToolActionCommandIn(
                TelegramUserId: query.From.Id,
                TelegramChatId: update.Chat.Id,
                TelegramMessageId: widgetMessageId,
                ActionId: actionId,
                IsConfirmed: isConfirmed,
                Locale: query.From.LanguageCode.OrEmpty())
            {
                WidgetQueryId = query.Id,
                Edit = edit
            };
        }

        var callback = update.CallbackQuery;
        var message = callback?.Message;
        var data = callback?.Data;

        if (callback is null || message is null || data is null ||
            data.StartsWith(AgentChatReplySender.CallbackPrefix, StringComparison.Ordinal) is false)
        {
            return default;
        }

        var value = data.AsSpan(AgentChatReplySender.CallbackPrefix.Length);
        if (value.Length < 3 || value[1] is not ':' || value[0] is not ('c' or 'x'))
        {
            return default;
        }

        return new AgentToolActionCommandIn(
            TelegramUserId: callback.From.Id,
            TelegramChatId: message.Chat.Id,
            TelegramMessageId: message.MessageId,
            ActionId: value[2..].ToString(),
            IsConfirmed: value[0] is 'c',
            Locale: callback.From.LanguageCode.OrEmpty());
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentToolActionCommandIn, Unit> request, CancellationToken cancellationToken)
    {
        var context = request.Context;
        var input = request.Value;
        var localizer = context.GetLocalizer(BaseName);
        var user = new AgentUserContext(input.TelegramUserId, input.TelegramChatId);
        var locale = context.GetAgentLocale(input.Locale);

        // A widget waits for the answer for 15 seconds only, and the action may take longer:
        // the query is answered at once, and the card gets the result through its state
        await TryAnswerWidgetQueryAsync(context, input.WidgetQueryId, cancellationToken).ConfigureAwait(false);

        AgentActionOutcome outcome;
        try
        {
            outcome = input.IsConfirmed
                ? await context.WithTypingAsync(
                    token => chatFeature.Service.ConfirmActionAsync(user, input.ActionId, input.Edit, locale, token).AsTask(),
                    cancellationToken).ConfigureAwait(false)
                : await chatFeature.Service.CancelActionAsync(user, input.ActionId, cancellationToken).ConfigureAwait(false);
        }
        catch (AgentUserNotSignedInException)
        {
            // The action is kept, so the buttons stay: the user can confirm it after signing in again
            await TrySetWidgetStateAsync(context, input, AgentActionWidget.StatusRetry, cancellationToken).ConfigureAwait(false);
            _ = await context.Api.SendHtmlModeTextAsync(localizer[SignInRequired], cancellationToken).ConfigureAwait(false);
            return context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentToolActionCommand>().LogError(exception, "Failed to process the agent action decision");
            await TrySetWidgetStateAsync(context, input, AgentActionWidget.StatusFailed, cancellationToken).ConfigureAwait(false);
            _ = await context.Api.SendHtmlModeTextAsync(localizer[UnexpectedError], cancellationToken).ConfigureAwait(false);

            return context.CreateCancelledResult<Unit>();
        }

        if (input.WidgetQueryId is null)
        {
            await TryRemoveKeyboardAsync(context, input.TelegramMessageId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var widgetStatus = outcome.Status switch
            {
                AgentActionStatus.Cancelled => AgentActionWidget.StatusCancelled,
                AgentActionStatus.Succeeded or AgentActionStatus.PartiallySucceeded => AgentActionWidget.StatusConfirmed,
                AgentActionStatus.Failed => AgentActionWidget.StatusFailed,
                _ => AgentActionWidget.StatusNotFound
            };

            await TrySetWidgetStateAsync(context, input, widgetStatus, cancellationToken).ConfigureAwait(false);
        }

        if (outcome.UserSettingsChanged)
        {
            CultureMiddleware.Invalidate(input.TelegramChatId);
        }

        if (outcome.Reply is not null)
        {
            await AgentChatReplySender.SendAsync(context, outcome.Reply, locale, cancellationToken).ConfigureAwait(false);
            return context.CreateCompleteResult<Unit>(default);
        }

        var text = outcome.Status switch
        {
            AgentActionStatus.Cancelled => localizer[Cancelled],
            AgentActionStatus.Succeeded => localizer[Succeeded],
            AgentActionStatus.PartiallySucceeded => localizer[PartiallySucceeded],
            AgentActionStatus.Failed => localizer[Failed],
            _ => localizer[NotFound]
        };

        _ = await context.Api.SendHtmlModeTextAsync(text, cancellationToken).ConfigureAwait(false);
        return context.CreateCompleteResult<Unit>(default);
    }

    private static async ValueTask TryAnswerWidgetQueryAsync(IChatContext context, string? widgetQueryId, CancellationToken cancellationToken)
    {
        if (widgetQueryId is null)
        {
            return;
        }

        try
        {
            await context.Api.AnswerWidgetQueryAsync(widgetQueryId, data: null, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentToolActionCommand>().LogWarning(exception, "Failed to answer the query of the agent action widget");
        }
    }

    // The state makes the card show the result and hide its buttons
    private static async ValueTask TrySetWidgetStateAsync(
        IChatContext context, AgentToolActionCommandIn input, string status, CancellationToken cancellationToken)
    {
        if (input.WidgetQueryId is null)
        {
            return;
        }

        try
        {
            await context.Api.EditMessageWidgetDataAsync(
                new(input.TelegramMessageId)
                {
                    ChatData = AgentActionWidget.BuildChatData(status, input.Edit)
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentToolActionCommand>().LogWarning(exception, "Failed to change the state of the agent action widget");
        }
    }

    private static async ValueTask TryRemoveKeyboardAsync(IChatContext context, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            _ = await context.Api.EditMessageReplyMarkupAsync(
                new(messageId) { ReplyMarkup = new() { InlineKeyboard = [] } },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentToolActionCommand>().LogWarning(exception, "Failed to remove the agent action keyboard");
        }
    }
}
