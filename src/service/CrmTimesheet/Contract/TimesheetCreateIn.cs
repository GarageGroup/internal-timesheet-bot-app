using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetCreateIn
{
    public TimesheetCreateIn(
        Guid userId,
        DateOnly date,
        Guid projectId,
        TimesheetProjectType projectType,
        decimal duration,
        [AllowNull] string description,
        TimesheetChannel channel)
    {
        UserId = userId;
        Date = date;
        Description = description.OrNullIfEmpty();
        Duration = duration;
        ProjectId = projectId;
        ProjectType = projectType;
        Channel = channel;
    }

    public Guid UserId { get; }

    public DateOnly Date { get; }

    public Guid ProjectId { get; }

    public TimesheetProjectType ProjectType { get; }

    public decimal Duration { get; }

    public string? Description { get; }

    public TimesheetChannel Channel { get; }
}