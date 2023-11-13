using System;

namespace GarageGroup.Internal.Timesheet;

internal interface ITimesheetProjectType
{
    public Guid Id { get; }

    public string? Name { get; }

    public TimesheetProjectType Type { get; }
}