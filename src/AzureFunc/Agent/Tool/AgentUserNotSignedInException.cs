using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentUserNotSignedInException(string audience)
    : Exception($"The user has no access token for the audience '{audience}'.")
{
    public string Audience { get; } = audience;
}
