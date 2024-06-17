namespace GarageGroup.Internal.Timesheet;

public record struct AuthenticationContext
{
    public User User { get; init; }
}