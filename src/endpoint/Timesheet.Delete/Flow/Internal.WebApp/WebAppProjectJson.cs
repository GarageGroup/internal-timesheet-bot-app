using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppProjectJson
{
    public string? Name { get; init; }

    [JsonPropertyName("typeName")]
    public string? TypeDisplayName { get; init; }
}