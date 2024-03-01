using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record DeleteTimesheetOptions
{
    public required TimeSpan TimesheetInterval { get; init; }
    public required string UrlWebApp { get; init; }
}