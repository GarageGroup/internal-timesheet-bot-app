using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct LastProjectSetGetOut
{
    public required FlatArray<ProjectSetGetItem> Projects { get; init; }
}