using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record ChannelDataJson
{
    [JsonProperty("method")]
    public string? Method { get; init; }

    [JsonProperty("parameters")]
    public ParameterJson? Parameters { get; init; }
}