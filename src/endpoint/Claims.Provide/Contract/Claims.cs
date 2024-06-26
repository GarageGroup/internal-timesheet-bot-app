using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class Claims
{
    public required Guid CorrelationId { get; init; }
    
    public required Guid SystemUserId { get; init; }
}