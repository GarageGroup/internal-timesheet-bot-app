using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentChatRequest(AgentUserContext User, string Text, string Locale);

internal sealed record class AgentChatReply(string Text);

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
}
