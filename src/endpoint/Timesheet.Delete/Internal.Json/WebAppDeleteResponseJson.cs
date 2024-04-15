namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppDeleteResponseJson
{
    public TimesheetJson? Timesheet { get; init; }

    public string? Date { get; init; }

    public string? Command { get; init; }
}