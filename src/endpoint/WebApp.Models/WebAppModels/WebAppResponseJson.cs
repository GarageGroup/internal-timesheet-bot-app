using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

public sealed record WebAppResponseJson
{
    [JsonProperty("message")]
    public MessageWebAppJson? Message { get; init; }
}