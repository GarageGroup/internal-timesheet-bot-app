using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct TimesheetTagSetGetOut
{
    public required FlatArray<string> Tags { get; init; }
}