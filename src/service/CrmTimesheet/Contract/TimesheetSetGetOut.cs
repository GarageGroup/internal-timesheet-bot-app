using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct TimesheetSetGetOut
{
    public required FlatArray<TimesheetSetGetItem> Timesheets { get; init; }
}