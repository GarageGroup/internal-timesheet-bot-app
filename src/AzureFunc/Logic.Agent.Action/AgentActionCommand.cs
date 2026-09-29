using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentActionCommand(IAgentActionApi agentApi)
    : IChatCommand<AgentActionCommandIn, Unit>, IChatCommandParser<AgentActionCommandIn>
{
    private const string CallbackPrefix = "agent-action:";

    public Optional<AgentActionCommandIn> Parse(ChatUpdate update)
    {
        var callback = update.CallbackQuery;
        var message = callback?.Message;

        if (callback is null || message is null || TryParseCallbackData(callback.Data, out var actionId, out var decision) is false)
        {
            return default;
        }

        return new AgentActionCommandIn(
            update.UpdateId,
            callback.From.Id,
            message.Chat.Id,
            message.MessageId,
            actionId,
            decision);
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentActionCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        AgentActionDecisionOut response;

        try
        {
            var input = request.Value;
            response = await agentApi.DecideAsync(
                input.ActionId,
                input.TelegramUpdateId,
                input.TelegramUserId,
                input.TelegramChatId,
                input.Decision,
                cancellationToken).ConfigureAwait(false);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound)
        {
            return await SendFailureAsync(request, "Действие не найдено или больше недоступно.", cancellationToken).ConfigureAwait(false);
        }
        catch (AgentMessageApiException exception) when (exception.StatusCode is HttpStatusCode.Conflict)
        {
            return await SendFailureAsync(
                request,
                "Действие уже обработано, истекло или его результат требует проверки. Проверьте списания перед повторной попыткой.",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentActionCommand>().LogError(exception, "Agent action decision request failed");

            return await SendFailureAsync(request, "Не удалось выполнить действие. Попробуйте позже.", cancellationToken).ConfigureAwait(false);
        }

        await RemoveKeyboardAsync(request, cancellationToken).ConfigureAwait(false);
        await SendResultAsync(
            request,
            response.Decision is AgentActionDecision.Confirm
                ? "✅ Время успешно списано."
                : "Списание отменено.",
            cancellationToken).ConfigureAwait(false);

        return request.Context.CreateCompleteResult<Unit>(default);
    }

    internal static string BuildCallbackData(Guid actionId, AgentActionDecision decision)
        =>
        string.Concat(
            CallbackPrefix,
            decision is AgentActionDecision.Confirm ? "c:" : "x:",
            actionId.ToString("N", CultureInfo.InvariantCulture));

    private static bool TryParseCallbackData(string? data, out Guid actionId, out AgentActionDecision decision)
    {
        actionId = default;
        decision = default;

        if (data is null || data.StartsWith(CallbackPrefix, StringComparison.Ordinal) is false)
        {
            return false;
        }

        var value = data.AsSpan(CallbackPrefix.Length);
        if (value.Length is not 34 || value[1] is not ':')
        {
            return false;
        }

        decision = value[0] switch
        {
            'c' => AgentActionDecision.Confirm,
            'x' => AgentActionDecision.Cancel,
            _ => (AgentActionDecision)(-1)
        };

        return Enum.IsDefined(decision) && Guid.TryParseExact(value[2..], "N", out actionId);
    }

    private static async ValueTask<ChatCommandResult<Unit>> SendFailureAsync(
        ChatCommandRequest<AgentActionCommandIn, Unit> request,
        string text,
        CancellationToken cancellationToken)
    {
        await RemoveKeyboardAsync(request, cancellationToken).ConfigureAwait(false);
        await SendResultAsync(request, text, cancellationToken).ConfigureAwait(false);

        return request.Context.CreateCancelledResult<Unit>();
    }

    private static async ValueTask RemoveKeyboardAsync(
        ChatCommandRequest<AgentActionCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await request.Context.Api.EditMessageReplyMarkupAsync(
                new(request.Value.TelegramMessageId) { ReplyMarkup = new() { InlineKeyboard = [] } },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentActionCommand>().LogWarning(exception, "Failed to remove agent action keyboard");
        }
    }

    private static async ValueTask SendResultAsync(
        ChatCommandRequest<AgentActionCommandIn, Unit> request,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await request.Context.Api.SendHtmlModeTextAsync(text, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentActionCommand>().LogWarning(exception, "Failed to send agent action result");
        }
    }
}
