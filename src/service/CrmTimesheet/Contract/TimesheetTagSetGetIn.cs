using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetTagSetGetIn
{
    public TimesheetTagSetGetIn(Guid userId, Guid projectId, DateOnly minDate)
    {
        UserId = userId;
        ProjectId = projectId;
        MinDate = minDate;
    }

    public Guid UserId { get; }

    public Guid ProjectId { get; }

    public DateOnly MinDate { get; }
}