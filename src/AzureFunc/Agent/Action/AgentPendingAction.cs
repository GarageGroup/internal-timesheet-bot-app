using System;

namespace GarageGroup.Internal.Timesheet;

// An argument of a prepared tool call. Display is a human readable value resolved from earlier tool results, if any
internal sealed record class AgentActionArgument(string Name, string Value, string? Display = null)
{
    // Fields of the object the value refers to, for example the date, project and duration of a time entry.
    // They are named like the arguments of the operation that creates such an object, so both are shown the same way
    public AgentActionArgument[]? Details { get; init; }
}

// A tool call that the model has requested and that waits for the user's confirmation
internal sealed record class AgentPendingAction(
    string Id,
    AgentUserContext User,
    string ToolName,
    string FunctionName,
    string ArgumentsJson,
    AgentActionArgument[] Arguments,
    DateTimeOffset ExpiresAt);
