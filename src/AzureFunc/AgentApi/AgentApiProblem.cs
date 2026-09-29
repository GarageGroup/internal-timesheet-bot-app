namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentApiProblem(
    string? Title,
    string? Detail,
    string? FailureCode,
    string? FailureMessage);
