using System;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct LastProjectSetGetIn
{
    public LastProjectSetGetIn(Guid userId, int top)
    {
        UserId = userId;
        Top = top;
    }

    public Guid UserId { get; }

    public int Top { get; }
}