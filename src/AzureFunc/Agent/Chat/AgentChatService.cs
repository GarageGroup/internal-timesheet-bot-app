using System;
using System.Collections.Generic;
using System.Linq;
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

    public async ValueTask<AgentActionOutcome> ConfirmActionAsync(
        AgentUserContext user, string actionId, string locale, CancellationToken cancellationToken)
    {
        // The tools are loaded first: if the user has to sign in again, the action stays available
        await using var toolSet = await toolLoader.LoadAsync(user, cancellationToken).ConfigureAwait(false);

        var action = await actionStore.TakeAsync(user.TelegramUserId, actionId, cancellationToken).ConfigureAwait(false);
        if (action is null || action.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return new(AgentActionStatus.NotFound);
        }

        var function = toolSet.Plugins
            .FirstOrDefault(plugin => string.Equals(plugin.Name, action.ToolName, StringComparison.Ordinal))?
            .FirstOrDefault(function => string.Equals(function.Name, action.FunctionName, StringComparison.Ordinal));

        var status = AgentActionStatus.Failed;
        string result;

        if (function is null)
        {
            result = "The operation is no longer available.";
        }
        else
        {
            try
            {
                var functionResult = await function.InvokeAsync(
                    new Kernel(), ReadArguments(action.ArgumentsJson), cancellationToken).ConfigureAwait(false);

                result = functionResult.ToString();
                status = AgentActionStatus.Succeeded;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "The confirmed agent action {tool}.{function} has failed", action.ToolName, action.FunctionName);
                result = "The request has failed: " + exception.Message;
            }
        }

        var eventText =
            $"[System event, not a user message] The user has confirmed the action '{action.FunctionName}' " +
            $"with the arguments {action.ArgumentsJson}. " +
            (status is AgentActionStatus.Succeeded ? "It has been executed. Result: " : "It has NOT been executed. Error: ") +
            Truncate(result) +
            " Tell the user the outcome in one short sentence. " +
            "If the action changed the time entries of a day, get the entries of that day and show them with the total.";

        var userSettingsChanged = status is AgentActionStatus.Succeeded &&
            option.UserSettingsFunctions.Contains(AgentToolInvocationFilter.GetKey(action.ToolName, action.FunctionName));

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
            $"[System event, not a user message] The user has cancelled the action '{action.FunctionName}'.",
            "The action has been cancelled and has not been executed.",
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
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                },
                kernel,
                cancellationToken).ConfigureAwait(false);

            reply = result.Content?.Trim();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AgentChatException(AgentChatFailureCode.Unknown, "The chat model request has failed.", exception);
        }

        if (filter.CapturedCall is AgentCapturedCall call)
        {
            var action = new AgentPendingAction(
                Id: Guid.NewGuid().ToString("N"),
                User: user,
                ToolName: call.ToolName,
                FunctionName: call.FunctionName,
                ArgumentsJson: call.ArgumentsJson,
                Arguments: call.Arguments,
                ExpiresAt: timeProvider.GetUtcNow().Add(option.ActionTtl));

            await actionStore.SaveAsync(action, cancellationToken).ConfigureAwait(false);

            await historyStore.AppendAsync(
                user,
                text,
                $"I have prepared the action '{call.FunctionName}' with the arguments {call.ArgumentsJson}. " +
                "It is shown to the user and waits for the confirmation by a button. It has not been executed yet.",
                option.Message.MaxHistoryMessageCount,
                cancellationToken).ConfigureAwait(false);

            return new(string.Empty, action)
            {
                UserSettingsChanged = filter.UserSettingsChanged
            };
        }

        if (string.IsNullOrEmpty(reply))
        {
            throw new AgentChatException(AgentChatFailureCode.EmptyResponse, "The chat model has returned an empty response.");
        }

        await historyStore.AppendAsync(
            user, text, reply, option.Message.MaxHistoryMessageCount, cancellationToken).ConfigureAwait(false);

        return new(reply)
        {
            UserSettingsChanged = filter.UserSettingsChanged
        };
    }

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

    private static string Truncate(string value)
        =>
        value.Length > MaxToolResultLength ? value[..MaxToolResultLength] : value;
}
