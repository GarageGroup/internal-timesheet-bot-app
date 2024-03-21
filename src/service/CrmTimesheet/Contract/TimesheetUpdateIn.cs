using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetUpdateIn
{
    public TimesheetUpdateIn(
        [AllowNull] TimesheetProjectIn? project,
        [AllowNull] decimal? duration,
        [AllowNull] string description,
        Guid timesheetId)
    {
        Description = description.OrNullIfEmpty();
        Duration = duration;
        Project = project;
        TimesheetId = timesheetId;
    }

    public TimesheetProjectIn? Project { get; }

    public decimal? Duration { get; }

    public string? Description { get; }

    public Guid TimesheetId { get; }
}