using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record MessageResponseJson
{
    [JsonProperty("text")]
    public string? Text { get; init; }
}