using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetProjectState
{
    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("type")]
    public TimesheetProjectType Type { get; init; }

    [JsonProperty("name")]
    public string? Name { get; init; }
}