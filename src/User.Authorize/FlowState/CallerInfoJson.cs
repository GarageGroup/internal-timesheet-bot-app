namespace GGroupp.Internal.Timesheet;

internal sealed record class CallerInfoJson
{
    public string? CallerServiceUrl { get; init; }

    public string? Scope { get; init; }
}