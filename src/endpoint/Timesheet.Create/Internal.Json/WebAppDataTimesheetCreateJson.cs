using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal readonly record struct WebAppDataTimesheetCreateJson
{
    [JsonProperty("id")]
    public Guid? Id { get; init; }

    [JsonProperty("duration")]
    public decimal Duration { get; init; }

    [JsonProperty("project")]
    public TimesheetProjectState? Project { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("date")]
    public string? Date { get; init; }
}