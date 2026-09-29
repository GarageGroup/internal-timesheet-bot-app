using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static AgentActionResource;

internal sealed class AgentActionCommand(IAgentActionApi agentApi)
    : IChatCommand<AgentActionCommandIn, Unit>, IChatCommandParser<AgentActionCommandIn>
{
    private const string CallbackPrefix = "agent-action:";

    public Optional<AgentActionCommandIn> Parse(ChatUpdate update)
    {
        var callback = update.CallbackQuery;
        var message = callback?.Message;

        if (callback is null || message is null ||
            TryParseCallbackData(callback.Data, out var actionId, out var actionType, out var decision) is false)
        {
            return default;
        }

        return new AgentActionCommandIn(
            update.UpdateId,
            callback.From.Id,
            message.Chat.Id,
            message.MessageId,
            actionId,
            actionType,
            decision);
    }

    public async ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<AgentActionCommandIn, Unit> request,
        CancellationToken cancellationToken)
    {
        var localizer = request.Context.GetLocalizer(BaseName);
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
        catch (AgentMessageApiException exception)
        {
            return await SendFailureAsync(
                request,
                GetFailureMessage(exception, request.Value.ActionType, localizer),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            request.Context.GetLogger<AgentActionCommand>().LogError(exception, "Agent action decision request failed");

            return await SendFailureAsync(request, localizer[UnexpectedError], cancellationToken).ConfigureAwait(false);
        }

        await RemoveKeyboardAsync(request, cancellationToken).ConfigureAwait(false);
        await SendResultAsync(
            request,
            GetSuccessMessage(request.Value.ActionType, response.Decision, localizer),
            cancellationToken).ConfigureAwait(false);

        return request.Context.CreateCompleteResult<Unit>(default);
    }

    internal static string BuildCallbackData(Guid actionId, AgentActionType actionType, AgentActionDecision decision)
        =>
        string.Concat(
            CallbackPrefix,
            decision is AgentActionDecision.Confirm ? "c:" : "x:",
            actionType is AgentActionType.Create ? "c:" : "d:",
            actionId.ToString("N", CultureInfo.InvariantCulture));

    private static string GetSuccessMessage(
        AgentActionType actionType,
        AgentActionDecision decision,
        IStringLocalizer localizer)
        =>
        (actionType, decision) switch
        {
            (AgentActionType.Create, AgentActionDecision.Confirm) => localizer[CreateConfirmSuccess],
            (AgentActionType.Delete, AgentActionDecision.Confirm) => localizer[DeleteConfirmSuccess],
            (AgentActionType.Create, AgentActionDecision.Cancel) => localizer[CreateCancelSuccess],
            _ => localizer[DeleteCancelSuccess]
        };

    private static string GetFailureMessage(
        AgentMessageApiException exception,
        AgentActionType actionType,
        IStringLocalizer localizer)
    {
        if (IsFailure(exception, "UserNotLinked", "Telegram user is not linked"))
        {
            return localizer[ProfileNotFound];
        }

        if (IsFailure(exception, "UserUnavailable", "Telegram user binding is unavailable"))
        {
            return localizer[ProfileUnavailable];
        }

        if (IsFailure(exception, "WriteDisabled", "Agent write operations are disabled"))
        {
            return localizer[WriteDisabled];
        }

        if (IsFailure(exception, "ActionExpired", "Agent action has expired"))
        {
            return localizer[ActionExpired];
        }

        if (IsFailure(exception, "InvalidActionState", "Agent action is not pending") ||
            IsFailure(exception, "ActionConflict", "Agent action was changed by another request"))
        {
            return localizer[ActionProcessed];
        }

        if (IsFailure(exception, "Indeterminate", "Timesheet creation result is indeterminate"))
        {
            return actionType is AgentActionType.Create
                ? localizer[CreateIndeterminate]
                : localizer[DeleteIndeterminate];
        }

        if (IsFailure(exception, "InvalidTimesheet", "Timesheet data is invalid"))
        {
            return IsFutureDateProblem(exception.ProblemDetail)
                ? localizer[FutureDate]
                : actionType is AgentActionType.Create
                    ? localizer[InvalidCreate]
                    : localizer[InvalidDelete];
        }

        if (IsFailure(exception, "TimesheetForbidden", "Timesheet creation is forbidden"))
        {
            return localizer[Forbidden];
        }

        if (IsFailure(exception, "ProjectNotFound", "Timesheet project was not found"))
        {
            return localizer[ProjectNotFound];
        }

        if (IsFailure(exception, "ActionNotFound", "Agent action was not found") ||
            exception.StatusCode is HttpStatusCode.NotFound)
        {
            return localizer[ActionNotFound];
        }

        if (exception.StatusCode is HttpStatusCode.Conflict)
        {
            return localizer[Conflict];
        }

        if (exception.StatusCode is HttpStatusCode.BadRequest)
        {
            return localizer[BadRequest];
        }

        if (exception.StatusCode is HttpStatusCode.Forbidden)
        {
            return localizer[ForbiddenFallback];
        }

        return localizer[UnexpectedError];
    }

    private static bool TryParseCallbackData(
        string? data,
        out Guid actionId,
        out AgentActionType actionType,
        out AgentActionDecision decision)
    {
        actionId = default;
        actionType = default;
        decision = default;

        if (data is null || data.StartsWith(CallbackPrefix, StringComparison.Ordinal) is false)
        {
            return false;
        }

        var value = data.AsSpan(CallbackPrefix.Length);
        if (value.Length is not 36 || value[1] is not ':' || value[3] is not ':')
        {
            return false;
        }

        decision = value[0] switch
        {
            'c' => AgentActionDecision.Confirm,
            'x' => AgentActionDecision.Cancel,
            _ => (AgentActionDecision)(-1)
        };

        actionType = value[2] switch
        {
            'c' => AgentActionType.Create,
            'd' => AgentActionType.Delete,
            _ => (AgentActionType)(-1)
        };

        return Enum.IsDefined(decision) &&
            Enum.IsDefined(actionType) &&
            Guid.TryParseExact(value[4..], "N", out actionId);
    }

    private static bool IsFutureDateProblem(string? detail)
        =>
        detail?.Contains("date cannot be in the future", StringComparison.OrdinalIgnoreCase) is true;

    private static bool IsFailure(AgentMessageApiException exception, string failureCode, string problemDetail)
        =>
        string.Equals(exception.FailureCode, failureCode, StringComparison.Ordinal) ||
        string.Equals(exception.ProblemDetail, problemDetail, StringComparison.Ordinal);

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
