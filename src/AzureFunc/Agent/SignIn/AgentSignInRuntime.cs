namespace GarageGroup.Internal.Timesheet;

// Service is null when user sign-in is not configured
internal sealed record class AgentSignInRuntime(AgentSignInService? Service);
