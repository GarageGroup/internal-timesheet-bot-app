using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct LastProjectSetGetIn
{
    public LastProjectSetGetIn(Guid userId, int top, DateOnly minDate)
    {
        UserId = userId;
        Top = top;
        MinDate = minDate;
    }

    public Guid UserId { get; }

    public int Top { get; }

    public DateOnly MinDate { get; }
}