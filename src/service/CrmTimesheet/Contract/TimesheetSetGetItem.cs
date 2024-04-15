using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
        decimal duration,
        Guid projectId,
        TimesheetProjectType projectType,
        [AllowNull] string projectName,
        [AllowNull] string description,
        Guid id)
    { 
        ProjectId = projectId;
        ProjectType = projectType;
        ProjectName = projectName.OrEmpty();
        Duration = duration;
        Description = description.OrEmpty();
        Id = id;
    }

    public Guid ProjectId { get; }

    public TimesheetProjectType ProjectType { get; }

    public string ProjectName { get; }

    public decimal Duration { get; }

    public string Description { get; }

    public Guid Id{ get; }
}