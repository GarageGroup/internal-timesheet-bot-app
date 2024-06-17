using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

public sealed record class ClaimsProvideOut
{
    [JsonPropertyName("data")]
    [JsonBodyOut]
    public required AuthenticationEventResponseData Data { get; init; }
}