using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetCreateIn
{
    public TimesheetCreateIn(
        Guid userId,
        DateOnly date,
        TimesheetProject project,
        decimal duration,
        [AllowNull] string description)
    {
        UserId = userId;
        Date = date;
        Description = description.OrNullIfEmpty();
        Duration = duration;
        Project = project;
    }

    public Guid UserId { get; }

    public DateOnly Date { get; }

    public TimesheetProject Project { get; }

    public decimal Duration { get; }

    public string? Description { get; }
}