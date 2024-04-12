using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetUpdateIn
{
    public TimesheetUpdateIn(
        Guid timesheetId,
        [AllowNull] TimesheetProjectIn? project,
        [AllowNull] decimal? duration,
        Optional<string> description)
    {
        Description = description;
        Duration = duration;
        Project = project;
        TimesheetId = timesheetId;
    }

    public Guid TimesheetId { get; }

    public TimesheetProjectIn? Project { get; }

    public decimal? Duration { get; }

    public Optional<string> Description { get; }
}