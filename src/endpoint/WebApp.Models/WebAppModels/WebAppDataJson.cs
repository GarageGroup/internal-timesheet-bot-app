using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record WebAppDataJson
{
    [JsonProperty("data")]
    public string? Data { get; init; }
}