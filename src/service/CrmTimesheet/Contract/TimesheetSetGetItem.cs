using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetSetGetItem
{
    public TimesheetSetGetItem(
        Guid timesheetId,
        DateOnly date,
        decimal duration,
        [AllowNull] string projectName,
        [AllowNull] string description)
    {
        TimesheetId = timesheetId;
        Date = date;
        Duration = duration;
        ProjectName = projectName.OrEmpty();
        Description = description.OrEmpty();
    }

    public Guid TimesheetId { get; }

    public DateOnly Date { get; }

    public decimal Duration { get; }

    public string ProjectName { get; }

    public string Description { get; }
}