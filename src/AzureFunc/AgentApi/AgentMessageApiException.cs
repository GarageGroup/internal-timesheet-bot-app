using System;
using System.Net;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentMessageApiException(HttpStatusCode statusCode)
    : Exception($"Agent API returned HTTP status code {(int)statusCode}.")
{
    internal HttpStatusCode StatusCode { get; } = statusCode;
}
