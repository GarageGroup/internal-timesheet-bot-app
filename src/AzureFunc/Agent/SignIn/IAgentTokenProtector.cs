namespace GarageGroup.Internal.Timesheet;

// A protected value is bound to its user: a value that is copied to another user cannot be unprotected
internal interface IAgentTokenProtector
{
    string Protect(string value, long telegramUserId);

    // Returns null if the value was protected with another key, for another user or was changed
    string? Unprotect(string protectedValue, long telegramUserId);
}
