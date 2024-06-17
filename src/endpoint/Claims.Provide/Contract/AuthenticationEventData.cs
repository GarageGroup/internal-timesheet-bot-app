namespace GarageGroup.Internal.Timesheet;

public sealed record class AuthenticationEventData
{
    public AuthenticationContext? AuthenticationContext { get; init; }
}