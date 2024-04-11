using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppDeleteResponseJson
{
    public Guid[]? Timesheets { get; init; }

    public string? Date { get; init; }

    public string? Command { get; init; }
}