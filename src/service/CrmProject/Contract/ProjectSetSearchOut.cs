using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct ProjectSetSearchOut
{
    public required FlatArray<ProjectSetGetItem> Projects { get; init; }
}