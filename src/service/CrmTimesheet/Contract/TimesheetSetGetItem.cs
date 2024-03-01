using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
        decimal duration,
        [AllowNull] string projectName,
        [AllowNull] string description,
        Guid id)
    {
        Duration = duration;
        ProjectName = projectName.OrEmpty();
        Description = description.OrEmpty();
        Id = id;
    }

    public decimal Duration { get; }

    public string ProjectName { get; }

    public string Description { get; }

    public Guid Id{ get; }
}