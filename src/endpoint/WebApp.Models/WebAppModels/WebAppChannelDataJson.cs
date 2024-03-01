using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record WebAppChannelDataJson
{
    [JsonProperty("method")]
    public string? Method { get; init; }

    [JsonProperty("parameters")]
    public ParametersJson? Parameters { get; init; }
}