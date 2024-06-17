namespace GarageGroup.Internal.Timesheet;

public sealed record class AuthenticationContext
{
    public User? User { get; init; }
}