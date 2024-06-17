using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

public sealed record class AuthenticationEventResponseData
{
    [JsonPropertyName("@odata.type")]
    public string ODataType { get; } = "microsoft.graph.onTokenIssuanceStartResponseData";
    
    [JsonPropertyName("actions")]
    public required FlatArray<TokenIssuanceAction>? Actions { get; init; }
}