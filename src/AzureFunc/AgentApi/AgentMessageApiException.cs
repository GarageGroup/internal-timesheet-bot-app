using System;
using System.Net;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentMessageApiException(
    HttpStatusCode statusCode,
    string? problemTitle = null,
    string? problemDetail = null,
    string? failureCode = null)
    : Exception($"Agent API returned HTTP status code {(int)statusCode}.")
{
    internal HttpStatusCode StatusCode { get; } = statusCode;

    internal string? ProblemTitle { get; } = problemTitle;

    internal string? ProblemDetail { get; } = problemDetail;

    internal string? FailureCode { get; } = failureCode;
}
