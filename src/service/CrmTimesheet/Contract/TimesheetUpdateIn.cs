using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetUpdateIn
{
    public TimesheetUpdateIn(
        Guid timesheetId,
        DateOnly date,
        TimesheetProject project,
        decimal duration,
        [AllowNull] string description)
    {
        TimesheetId = timesheetId;
        Date = date;
        Project = project;
        Duration = duration;
        Description = description.OrNullIfEmpty();
    }

    public Guid TimesheetId { get; }

    public DateOnly Date { get; }

    public TimesheetProject Project { get; }

    public decimal Duration { get; }

    public string? Description { get; }
}