namespace GarageGroup.Internal.Timesheet;

internal interface IAgentTokenProtector
{
    string Protect(string value);

    // Returns null if the value was protected with another key or was changed
    string? Unprotect(string protectedValue);
}
