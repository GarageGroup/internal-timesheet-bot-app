using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentOption
{
    public bool Enabled { get; init; }

    public required AgentFoundryOption Foundry { get; init; }

    public required AgentMessageOption Message { get; init; }

    public required AgentStorageOption Storage { get; init; }

    public AgentToolOption[] Tools { get; init; } = [];

    // Null when user sign-in is not configured
    public AgentSignInOption? SignIn { get; init; }
}

internal sealed record class AgentFoundryOption(Uri ProjectEndpoint, string ModelId, string TokenScope)
{
    public string? ManagedIdentityClientId { get; init; }
}

internal sealed record class AgentMessageOption(TimeZoneInfo TimeZone, int MaxTextLength, int MaxHistoryMessageCount)
{
    // How long a prepared action waits for the user's confirmation
    public TimeSpan ActionTtl { get; init; } = TimeSpan.FromMinutes(10);
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

    // Operation ids (OpenAPI) or tool names (MCP) that are executed only after the user confirms them with a button
    public string[] ConfirmationOperations { get; init; } = [];
}
