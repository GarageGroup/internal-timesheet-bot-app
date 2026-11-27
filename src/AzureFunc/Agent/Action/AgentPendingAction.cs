using System;

namespace GarageGroup.Internal.Timesheet;

// An argument of a prepared tool call. Display is a human readable value resolved from earlier tool results, if any
internal sealed record class AgentActionArgument(string Name, string Value, string? Display = null)
{
    // Fields of the object the value refers to, for example the date, project and duration of a time entry.
    // They are named like the arguments of the operation that creates such an object, so both are shown the same way
    public AgentActionArgument[]? Details { get; init; }
}

// A tool call that the model has requested: the function, its arguments as JSON and the same arguments for the preview
internal sealed record class AgentActionCall(string ToolName, string FunctionName, string ArgumentsJson, AgentActionArgument[] Arguments);

// Tool calls that the model has requested in one turn and that wait for the user's confirmation.
// The user confirms or cancels all of them at once
internal sealed record class AgentPendingAction(
    string Id,
    AgentUserContext User,
    AgentActionCall[] Calls,
    DateTimeOffset ExpiresAt);
