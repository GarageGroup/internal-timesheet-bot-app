using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetProjectCommand
{
    public Guid Id { get; init; }

    public string? Name { get; init; }

    public TimesheetProjectType Type { get; init; }
}