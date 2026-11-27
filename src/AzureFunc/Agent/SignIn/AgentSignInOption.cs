using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentSignInOption(string TenantId, string ClientId, Uri RedirectUri, byte[] TokenEncryptionKey)
{
    // Client ID of the user-assigned Managed Identity that is registered as a federated credential of the application
    public string? ManagedIdentityClientId { get; init; }

    public string TableName { get; init; } = "TimesheetBotAgentSignIn";

    public TimeSpan PendingTtl { get; init; } = TimeSpan.FromMinutes(10);
}
