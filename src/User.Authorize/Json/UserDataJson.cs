using System;
using System.Text.Json.Serialization;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct UserDataJson
{
    [JsonPropertyName("activeDirectoryId")]
    public Guid ActiveDirectoryId { get; init; }

    [JsonPropertyName("dataverseUserId")]
    public Guid DataverseUserId { get; init; }

    [JsonPropertyName("dataverseUserFirstName")]
    public string? DataverseUserFirstName { get; init; }

    [JsonPropertyName("dataverseUserLastName")]
    public string? DataverseUserLastName { get; init; }

    [JsonPropertyName("dataverseUserFullName")]
    public string? DataverseUserFullName { get; init; }
}