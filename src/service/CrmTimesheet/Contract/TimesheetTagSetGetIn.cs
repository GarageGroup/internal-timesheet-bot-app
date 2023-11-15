using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetTagSetGetIn
{
    public TimesheetTagSetGetIn(Guid userId, Guid projectId, DateOnly minDate, DateOnly maxDate)
    {
        UserId = userId;
        ProjectId = projectId;
        MinDate = minDate;
        MaxDate = maxDate;
    }

    public Guid UserId { get; }

    public Guid ProjectId { get; }

    public DateOnly MinDate { get; }

    public DateOnly MaxDate { get; }
}