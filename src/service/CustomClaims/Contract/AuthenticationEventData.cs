namespace GarageGroup.Internal.Timesheet;

public record struct AuthenticationEventData
{
    public AuthenticationContext AuthenticationContext { get; init; }
}