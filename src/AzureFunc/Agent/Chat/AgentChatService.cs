using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GarageGroup.Internal.Timesheet;

// NoConfirmationFunctions contains '<tool>.<function>' keys of the tool calls the model may execute right away.
// Every other call is executed only after the user's confirmation
internal sealed record class AgentChatServiceOption(
    AgentMessageOption Message, IReadOnlySet<string> NoConfirmationFunctions, TimeSpan ActionTtl)
{
    // '<tool>.<function>' keys of the tool calls that change the user settings kept by the bot
    public IReadOnlySet<string> UserSettingsFunctions { get; init; } = new HashSet<string>();
}

internal sealed class AgentChatService(
    IAgentKernelFactory kernelFactory,
    IAgentToolLoader toolLoader,
    IAgentHistoryStore historyStore,
    IAgentActionStore actionStore,
    AgentEntityCache entityCache,
    AgentPromptProvider promptProvider,
    AgentChatServiceOption option,
    TimeProvider timeProvider,
    ILogger logger)
    : IAgentChatService
{
    private const int MaxToolResultLength = 4000;

    public async ValueTask<AgentChatReply> SendAsync(AgentChatRequest request, CancellationToken cancellationToken)
    {
        var text = request.Text.Trim();
        if (text.Length > option.Message.MaxTextLength)
        {
            throw new AgentChatException(
                AgentChatFailureCode.MessageTooLong, $"Message text must not exceed {option.Message.MaxTextLength} characters.");
        }

        await using var toolSet = await toolLoader.LoadAsync(request.User, cancellationToken).ConfigureAwait(false);
        return await RunTurnAsync(request.User, toolSet, text, request.Locale, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<AgentActionOutcome> ConfirmActionAsync(
        AgentUserContext user, string actionId, string locale, CancellationToken cancellationToken)
        =>
        ConfirmActionAsync(user, actionId, edit: null, locale, cancellationToken);

    public async ValueTask<AgentActionOutcome> ConfirmActionAsync(
        AgentUserContext user, string actionId, AgentActionEdit? edit, string locale, CancellationToken cancellationToken)
    {
        // The tools are loaded first: if the user has to sign in again, the action stays available
        await using var toolSet = await toolLoader.LoadAsync(user, cancellationToken).ConfigureAwait(false);

        var action = await actionStore.TakeAsync(user.TelegramUserId, actionId, cancellationToken).ConfigureAwait(false);
        if (action is null || action.Calls.Length is 0 || action.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return new(AgentActionStatus.NotFound);
        }

        // The model is told about the call as it has been executed, with the values the user has changed
        action = AgentActionEditor.Apply(action, edit);

        // The calls do not depend on each other, so they run in parallel and a failed one does not stop the rest
        var results = await Task.WhenAll(
            action.Calls.Select(call => InvokeCallAsync(toolSet, call, cancellationToken))).ConfigureAwait(false);

        var succeededCount = results.Count(static result => result.IsSucceeded);
        var status = succeededCount switch
        {
            0 => AgentActionStatus.Failed,
            _ when succeededCount == results.Length => AgentActionStatus.Succeeded,
            _ => AgentActionStatus.PartiallySucceeded
        };

        var eventText = BuildConfirmedEventText(action.Calls, results);

        var userSettingsChanged = action.Calls
            .Where((_, index) => results[index].IsSucceeded)
            .Any(call => option.UserSettingsFunctions.Contains(AgentToolInvocationFilter.GetKey(call.ToolName, call.FunctionName)));

        try
        {
            var reply = await RunTurnAsync(user, toolSet, eventText, locale, cancellationToken).ConfigureAwait(false);
            return new(status, action, reply)
            {
                UserSettingsChanged = userSettingsChanged || reply.UserSettingsChanged
            };
        }
        catch (AgentChatException exception)
        {
            // The action has already been processed, so the user gets a predefined message instead of the model answer
            logger.LogWarning(exception, "Failed to get the model answer about the executed agent action");
            return new(status, action)
            {
                UserSettingsChanged = userSettingsChanged
            };
        }
    }

    public async ValueTask<AgentActionOutcome> CancelActionAsync(AgentUserContext user, string actionId, CancellationToken cancellationToken)
    {
        var action = await actionStore.TakeAsync(user.TelegramUserId, actionId, cancellationToken).ConfigureAwait(false);
        if (action is null)
        {
            return new(AgentActionStatus.NotFound);
        }

        await historyStore.AppendAsync(
            user,
            "[System event, not a user message] The user has cancelled the prepared actions: " + DescribeCalls(action.Calls),
            "The actions have been cancelled and have not been executed.",
            option.Message.MaxHistoryMessageCount,
            cancellationToken).ConfigureAwait(false);

        return new(AgentActionStatus.Cancelled, action);
    }

    private async ValueTask<AgentChatReply> RunTurnAsync(
        AgentUserContext user, AgentToolSet toolSet, string text, string locale, CancellationToken cancellationToken)
    {
        var kernel = kernelFactory.Create();
        kernel.Plugins.AddRange(toolSet.Plugins);

        var filter = new AgentToolInvocationFilter(
            option.NoConfirmationFunctions, option.UserSettingsFunctions, entityCache, user.TelegramUserId);
        kernel.AutoFunctionInvocationFilters.Add(filter);

        var history = new ChatHistory(await promptProvider.BuildAsync(locale, cancellationToken).ConfigureAwait(false));
        var storedMessages = await historyStore.GetAsync(
            user, option.Message.MaxHistoryMessageCount, cancellationToken).ConfigureAwait(false);

        foreach (var message in storedMessages)
        {
            if (message.IsUser)
            {
                history.AddUserMessage(message.Text);
            }
            else
            {
                history.AddAssistantMessage(message.Text);
            }
        }

        history.AddUserMessage(text);

        string? reply;
        try
        {
            var result = await kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(
                history,
                new PromptExecutionSettings
                {
                    // Several actions requested by the user come as several tool calls of one model answer
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { AllowParallelCalls = true })
                },
                kernel,
                cancellationToken).ConfigureAwait(false);

            reply = result.Content?.Trim();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AgentChatException(AgentChatFailureCode.Unknown, "The chat model request has failed.", exception);
        }

        if (filter.CapturedCalls.Count > 0)
        {
            var action = new AgentPendingAction(
                Id: Guid.NewGuid().ToString("N"),
                User: user,
                Calls: [.. filter.CapturedCalls],
                ExpiresAt: timeProvider.GetUtcNow().Add(option.ActionTtl));

            await actionStore.SaveAsync(action, cancellationToken).ConfigureAwait(false);

            await historyStore.AppendAsync(
                user,
                text,
                "I have prepared the actions: " + DescribeCalls(action.Calls) + " " +
                "They are shown to the user and wait for the confirmation by one button. They have not been executed yet.",
                option.Message.MaxHistoryMessageCount,
                cancellationToken).ConfigureAwait(false);

            return new(string.Empty, action)
            {
                UserSettingsChanged = filter.UserSettingsChanged,
                EditableCardToday = option.Message.EditableActionCard
                    ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), option.Message.TimeZone).DateTime)
                    : null
            };
        }

        if (string.IsNullOrEmpty(reply))
        {
            throw new AgentChatException(AgentChatFailureCode.EmptyResponse, "The chat model has returned an empty response.");
        }

        // The history keeps the restored answer too, so the next turns do not rely on a lossy copy
        reply = AgentTimesheetHtml.Restore(reply, filter.Timesheets);

        await historyStore.AppendAsync(
            user, text, reply, option.Message.MaxHistoryMessageCount, cancellationToken).ConfigureAwait(false);

        return new(reply)
        {
            UserSettingsChanged = filter.UserSettingsChanged
        };
    }

    private async Task<CallResult> InvokeCallAsync(AgentToolSet toolSet, AgentActionCall call, CancellationToken cancellationToken)
    {
        var function = toolSet.Plugins
            .FirstOrDefault(plugin => string.Equals(plugin.Name, call.ToolName, StringComparison.Ordinal))?
            .FirstOrDefault(function => string.Equals(function.Name, call.FunctionName, StringComparison.Ordinal));

        if (function is null)
        {
            return new(IsSucceeded: false, "The operation is no longer available.");
        }

        try
        {
            var result = await function.InvokeAsync(new Kernel(), ReadArguments(call.ArgumentsJson), cancellationToken).ConfigureAwait(false);
            return new(IsSucceeded: true, result.ToString());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The confirmed agent action {tool}.{function} has failed", call.ToolName, call.FunctionName);
            return new(IsSucceeded: false, DescribeFailure(exception));
        }
    }

    // The service explains a rejected request in the 'detail' of a problem response, for example a closed period.
    // The model gets this reason, so it can tell the user why the action has not been executed
    internal static string DescribeFailure(Exception exception)
    {
        if (exception is not HttpOperationException { ResponseContent: { Length: > 0 } content } httpException)
        {
            return "The request has failed: " + exception.Message;
        }

        var status = httpException.StatusCode is null ? string.Empty : $" with the status {(int)httpException.StatusCode}";
        return $"The service has rejected the request{status}. Reason: {ReadProblemDetail(content) ?? content.Trim()}";
    }

    private static string? ReadProblemDetail(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);

            if (document.RootElement.ValueKind is not JsonValueKind.Object ||
                document.RootElement.TryGetProperty("detail", out var detail) is false ||
                detail.ValueKind is not JsonValueKind.String)
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(detail.GetString()) ? null : detail.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildConfirmedEventText(AgentActionCall[] calls, CallResult[] results)
    {
        // The results share the length limit, so a large batch does not overflow the model context
        var maxResultLength = Math.Max(200, MaxToolResultLength / calls.Length);
        var builder = new StringBuilder("[System event, not a user message] The user has confirmed the prepared actions.");

        for (var index = 0; index < calls.Length; index++)
        {
            _ = builder
                .Append("\n").Append(index + 1).Append(". '").Append(calls[index].FunctionName)
                .Append("' with the arguments ").Append(calls[index].ArgumentsJson)
                .Append(results[index].IsSucceeded ? " — EXECUTED. Result: " : " — NOT executed. Error: ")
                .Append(Truncate(results[index].Text, maxResultLength));
        }

        var failedCount = results.Count(static result => result.IsSucceeded is false);
        _ = builder.Append("\n").Append(
            failedCount switch
            {
                0 => "All the actions have been executed. Tell the user the outcome in one short sentence.",
                _ when failedCount == calls.Length => "None of the actions has been executed. Tell the user so in one short sentence.",
                _ => $"{calls.Length - failedCount} of {calls.Length} actions have been executed. Tell the user how many have been executed " +
                    "and list each action that has NOT been executed, so the user can repeat it."
            });

        return builder
            .Append(" If the executed actions changed time entries, get the entries of the affected days and show them with the totals.")
            .ToString();
    }

    private static string DescribeCalls(AgentActionCall[] calls)
        =>
        string.Join("; ", calls.Select(static call => $"'{call.FunctionName}' with the arguments {call.ArgumentsJson}")) + ".";

    private sealed record class CallResult(bool IsSucceeded, string Text);

    private static KernelArguments ReadArguments(string argumentsJson)
    {
        var arguments = new KernelArguments();
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson) ?? [];

        foreach (var value in values)
        {
            arguments[value.Key] = value.Value.ValueKind switch
            {
                JsonValueKind.String => value.Value.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => value.Value
            };
        }

        return arguments;
    }

    private static string Truncate(string value, int maxLength)
        =>
        value.Length > maxLength ? value[..maxLength] : value;
}
