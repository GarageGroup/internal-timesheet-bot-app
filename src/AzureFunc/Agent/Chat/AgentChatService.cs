using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentChatService(
    IAgentKernelFactory kernelFactory,
    IAgentToolLoader toolLoader,
    IAgentHistoryStore historyStore,
    AgentPromptProvider promptProvider,
    AgentMessageOption option)
    : IAgentChatService
{
    public async ValueTask<AgentChatReply> SendAsync(AgentChatRequest request, CancellationToken cancellationToken)
    {
        var text = request.Text.Trim();
        if (text.Length > option.MaxTextLength)
        {
            throw new AgentChatException(
                AgentChatFailureCode.MessageTooLong, $"Message text must not exceed {option.MaxTextLength} characters.");
        }

        var kernel = kernelFactory.Create();

        await using var toolSet = await toolLoader.LoadAsync(request.User, cancellationToken).ConfigureAwait(false);
        kernel.Plugins.AddRange(toolSet.Plugins);

        var history = new ChatHistory(promptProvider.Build(request.Locale));
        var storedMessages = await historyStore.GetAsync(
            request.User, option.MaxHistoryMessageCount, cancellationToken).ConfigureAwait(false);

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

        if (string.IsNullOrEmpty(reply))
        {
            throw new AgentChatException(AgentChatFailureCode.EmptyResponse, "The chat model has returned an empty response.");
        }

        await historyStore.AppendAsync(
            request.User, text, reply, option.MaxHistoryMessageCount, cancellationToken).ConfigureAwait(false);

        return new(reply);
    }
}
