using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
        decimal duration,
        [AllowNull] string projectName,
        [AllowNull] string description)
    {
        Duration = duration;
        ProjectName = projectName.OrEmpty();
        Description = description.OrEmpty();
    }

    public decimal Duration { get; }

    public string ProjectName { get; }

    public string Description { get; }
}