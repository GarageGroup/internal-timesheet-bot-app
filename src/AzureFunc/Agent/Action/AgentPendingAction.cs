using System;

namespace GarageGroup.Internal.Timesheet;

// An argument of a prepared tool call. Display is a human readable value resolved from earlier tool results, if any
internal sealed record class AgentActionArgument(string Name, string Value, string? Display = null);

// A tool call that the model has requested and that waits for the user's confirmation
internal sealed record class AgentPendingAction(
    string Id,
    AgentUserContext User,
    string ToolName,
    string FunctionName,
    string ArgumentsJson,
    AgentActionArgument[] Arguments,
    DateTimeOffset ExpiresAt);
