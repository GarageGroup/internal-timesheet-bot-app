using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentOption
{
    public bool Enabled { get; init; }

    public required AgentFoundryOption Foundry { get; init; }

    public required AgentMessageOption Message { get; init; }

    public required AgentStorageOption Storage { get; init; }

    public AgentToolOption[] Tools { get; init; } = [];
}

internal sealed record class AgentFoundryOption(Uri ProjectEndpoint, string ModelId, string TokenScope)
{
    public string? ManagedIdentityClientId { get; init; }
}

internal sealed record class AgentMessageOption(TimeZoneInfo TimeZone, int MaxTextLength, int MaxHistoryMessageCount);

internal sealed record class AgentStorageOption(string ConnectionString, string ConversationTableName);

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

    // API Management subscription key. Sent both when the specification is loaded and when the API is called
    public string? SubscriptionKey { get; init; }

    // Operation ids (OpenAPI) or tool names (MCP) available to the model. All operations are available when empty
    public string[] AllowedOperations { get; init; } = [];
}
