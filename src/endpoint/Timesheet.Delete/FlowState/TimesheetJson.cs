using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    [JsonProperty("duration")]
    public decimal Duration { get; init; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("id")]
    public Guid Id { get; init; }
}