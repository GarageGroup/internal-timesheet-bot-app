using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentChatRequest(AgentUserContext User, string Text, string Locale);

// Action is set when the model has prepared tool calls that wait for the user's confirmation. Text is empty in this case
internal sealed record class AgentChatReply(string Text, AgentPendingAction? Action = null)
{
    // True when the user settings were changed in this turn, so the cached settings must be read again
    public bool UserSettingsChanged { get; init; }
}

internal enum AgentActionStatus
{
    // The action does not exist, has expired or has already been processed
    NotFound,

    Cancelled,

    Succeeded,

    // Some of the confirmed calls have been executed and some have failed
    PartiallySucceeded,

    Failed
}

// Reply is the model answer about the executed action. It is null if the model could not answer
internal sealed record class AgentActionOutcome(AgentActionStatus Status, AgentPendingAction? Action = null, AgentChatReply? Reply = null)
{
    public bool UserSettingsChanged { get; init; }
}

internal enum AgentChatFailureCode
{
    Unknown,

    MessageTooLong,

    EmptyResponse
}

internal sealed class AgentChatException(AgentChatFailureCode failureCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public AgentChatFailureCode FailureCode { get; } = failureCode;
}

internal interface IAgentChatService
{
    // Throws AgentUserNotSignedInException if the user must sign in and AgentChatException on other failures
    ValueTask<AgentChatReply> SendAsync(AgentChatRequest request, CancellationToken cancellationToken);

    // Executes the prepared tool calls in parallel. Throws AgentUserNotSignedInException if the user must sign in again
    ValueTask<AgentActionOutcome> ConfirmActionAsync(AgentUserContext user, string actionId, string locale, CancellationToken cancellationToken);

    ValueTask<AgentActionOutcome> CancelActionAsync(AgentUserContext user, string actionId, CancellationToken cancellationToken);
}
