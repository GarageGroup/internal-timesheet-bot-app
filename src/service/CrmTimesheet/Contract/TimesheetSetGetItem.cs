using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
        decimal duration,
        TimesheetProjectType projectType,
        [AllowNull] string projectName,
        [AllowNull] string description,
        Guid id)
    {
        Duration = duration;
        ProjectType = projectType;
        ProjectName = projectName.OrEmpty();
        Description = description.OrEmpty();
        Id = id;
    }

    public decimal Duration { get; }

    public TimesheetProjectType ProjectType { get; }

    public string ProjectName { get; }

    public string Description { get; }

    public Guid Id{ get; }
}