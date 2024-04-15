using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson
{
    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("type")]
    public TimesheetProjectType Type { get; init; }

    [JsonProperty("name")]
    public string? Name { get; init; }

    [JsonProperty("typeName")]
    public string? DisplayTypeName { get; init; }
}