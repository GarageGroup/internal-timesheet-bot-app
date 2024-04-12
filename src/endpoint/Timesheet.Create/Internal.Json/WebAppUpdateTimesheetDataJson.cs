using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet.Internal.Json;

internal sealed record class WebAppUpdateTimesheetDataJson
{
    [JsonProperty("duration")]
    public decimal? Duration { get; init; }

    [JsonProperty("projectType")]
    public TimesheetProjectType ProjectType { get; set; }

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