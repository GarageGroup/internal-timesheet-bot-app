using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class UpdateTimesheetJson
{
    [JsonProperty("duration")]
    public decimal? Duration { get; init; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("isEditProject")]
    public bool IsEditProject { get; init; }

    [JsonProperty("date")]
    public string? Date { get; init; }

    [JsonProperty("command")]
    public string? Command { get; init; }
}