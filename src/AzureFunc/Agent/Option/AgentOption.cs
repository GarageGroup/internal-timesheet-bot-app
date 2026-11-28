using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentOption
{
    public required AgentFoundryOption Foundry { get; init; }

    public required AgentMessageOption Message { get; init; }

    public required AgentStorageOption Storage { get; init; }

    public AgentToolOption[] Tools { get; init; } = [];

    // Null when user sign-in is not configured
    public AgentSignInOption? SignIn { get; init; }

    // Null when voice messages are not supported
    public AgentVoiceTranscriptionOption? Voice { get; init; }

    public AgentPromptOption Prompt { get; init; } = new();
}

// The blob that overrides the system prompt packaged with the application
internal sealed record class AgentPromptOption
{
    public string ContainerName { get; init; } = "agent";

    public string BlobName { get; init; } = "AgentSystem.md";

    // How long the prompt is kept in memory before the blob is read again
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}

// An Azure OpenAI speech-to-text deployment. It is called with the same Managed Identity as the chat model
internal sealed record class AgentVoiceTranscriptionOption(Uri Endpoint, string DeploymentName, string ModelId);

internal sealed record class AgentFoundryOption(Uri ProjectEndpoint, string ModelId, string TokenScope)
{
    public string? ManagedIdentityClientId { get; init; }
}

internal sealed record class AgentMessageOption(TimeZoneInfo TimeZone, int MaxTextLength, int MaxHistoryMessageCount)
{
    // How long a prepared action waits for the user's confirmation
    public TimeSpan ActionTtl { get; init; } = TimeSpan.FromMinutes(10);

    // An experiment: the confirmation of creating or changing a time entry is sent as a Telegram widget,
    // where the user can change the date and the duration before confirming
    public bool EditableActionCard { get; init; }
}

internal sealed record class AgentStorageOption(string ConnectionString, string ConversationTableName)
{
    public string ActionTableName { get; init; } = "TimesheetBotAgentAction";
}

internal enum AgentToolType
{
    OpenApi,

    Mcp
}

internal sealed record class AgentToolOption(string Name, AgentToolType Type, Uri Url)
{
    // Base address of the API behind API Management. Used only by OpenAPI tools
    public Uri? BaseAddress { get; init; }

    // Entra audience of the API. The user access token is requested for '<Audience>/.default'
    public string? Audience { get; init; }

    // API Management subscription key that is sent when the API or the MCP server is called
    public string? SubscriptionKey { get; init; }

    // API Management subscription key that is sent when the OpenAPI specification is loaded.
    // The specification is usually published by another API, so its key differs from the key of the API itself
    public string? SpecificationSubscriptionKey { get; init; }

    // Operation ids (OpenAPI) or tool names (MCP) available to the model. All operations are available when empty
    public string[] AllowedOperations { get; init; } = [];

    // Operations that are never available to the model, even if they are allowed
    public string[] ExcludedOperations { get; init; } = [];

    // Operations the model may execute right away. Every other operation is executed only after the user
    // confirms it with a button, so an operation added to the API later is safe by default. '*' means all operations
    public string[] NoConfirmationOperations { get; init; } = [];

    // Operations that change the user settings the bot keeps in memory, for example the language.
    // After such an operation the bot reads the user settings again
    public string[] UserSettingsOperations { get; init; } = [];
}
