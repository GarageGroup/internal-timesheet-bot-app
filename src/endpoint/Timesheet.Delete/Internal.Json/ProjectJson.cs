using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson
{
    [JsonProperty("name")]
    public string? Name { get; init; }

    [JsonProperty("typeName")]
    public string? DisplayTypeName { get; init; }
}