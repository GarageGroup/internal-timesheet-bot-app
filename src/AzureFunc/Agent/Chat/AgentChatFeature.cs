namespace GarageGroup.Internal.Timesheet;

// Everything the bot commands need from the agent
internal sealed record class AgentChatFeature(IAgentChatService Service)
{
    // Null when no agent tool requires a user access token
    public IAgentSignInService? SignIn { get; init; }

    // Null when voice messages are not supported
    public IAgentVoiceTranscriber? Voice { get; init; }
}
