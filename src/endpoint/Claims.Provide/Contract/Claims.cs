using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class Claims
{
    public required Guid SystemUserId { get; init; }
}