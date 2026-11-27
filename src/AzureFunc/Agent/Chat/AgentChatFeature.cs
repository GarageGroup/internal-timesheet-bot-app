namespace GarageGroup.Internal.Timesheet;

// The in-bot agent is used instead of the Timesheet agent API when it is enabled in the configuration
internal sealed record class AgentChatFeature(bool Enabled, IAgentChatService Service)
{
    // Null when user sign-in is not configured
    public IAgentSignInService? SignIn { get; init; }
}
