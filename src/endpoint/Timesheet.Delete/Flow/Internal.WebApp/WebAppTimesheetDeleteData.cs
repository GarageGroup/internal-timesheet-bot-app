using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppTimesheetDeleteData
{
    public WebAppTimesheetJson? Timesheet { get; init; }

    public DateOnly Date { get; init; }

    public string? Command { get; init; }
}