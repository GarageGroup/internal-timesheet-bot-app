using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record WebAppJson
{
    [JsonProperty("url")]
    public string? Url { get; init; }
}