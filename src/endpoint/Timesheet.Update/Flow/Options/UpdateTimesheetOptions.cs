using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record UpdateTimesheetOptions
{
    public required TimeSpan TimesheetInterval { get; init; }

    public required string UrlWebApp { get; init; }
}