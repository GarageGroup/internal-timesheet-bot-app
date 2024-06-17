using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class CustomClaims
{
    public required Guid SystemUserId { get; init; }
}