using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AuthenticationContext
{
    public Guid CorrelationId { get; init; }
    
    public User? User { get; init; }
}