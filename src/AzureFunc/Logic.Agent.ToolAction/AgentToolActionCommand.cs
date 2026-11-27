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

        AgentActionOutcome outcome;
        try
        {
            outcome = input.IsConfirmed
                ? await context.WithTypingAsync(
                    token => chatFeature.Service.ConfirmActionAsync(user, input.ActionId, locale, token).AsTask(),
                    cancellationToken).ConfigureAwait(false)
                : await chatFeature.Service.CancelActionAsync(user, input.ActionId, cancellationToken).ConfigureAwait(false);
        }
        catch (AgentUserNotSignedInException)
        {
            // The action is kept, so the buttons stay: the user can confirm it after signing in again
            _ = await context.Api.SendHtmlModeTextAsync(localizer[SignInRequired], cancellationToken).ConfigureAwait(false);
            return context.CreateCompleteResult<Unit>(default);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            context.GetLogger<AgentToolActionCommand>().LogError(exception, "Failed to process the agent action decision");
            _ = await context.Api.SendHtmlModeTextAsync(localizer[UnexpectedError], cancellationToken).ConfigureAwait(false);

            return context.CreateCancelledResult<Unit>();
        }

        await TryRemoveKeyboardAsync(context, input.TelegramMessageId, cancellationToken).ConfigureAwait(false);

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
