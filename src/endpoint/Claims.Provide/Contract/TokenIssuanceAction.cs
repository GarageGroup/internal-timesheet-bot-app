using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TokenIssuanceAction
{
    [JsonPropertyName("@odata.type")]
    public string ODataType { get; } = "microsoft.graph.tokenIssuanceStart.provideClaimsForToken";
    
    [JsonPropertyName("claims")]
    public required Claims Claims { get; init; }
}