using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("duration")]
    public decimal Duration { get; init; }

    [JsonProperty("project")]
    public ProjectJson? Project { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("incidentStateCode")]
    public StateCode? IncidentStateCode { get; init; }

    [JsonProperty("timesheetStateCode")]
    public StateCode TimesheetStateCode { get; init; }
}