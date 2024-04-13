using Newtonsoft.Json;
using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet.Internal.Json;

internal sealed record class WebAppUpdateTimesheetDataJson
{
    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("duration")]
    public decimal? Duration { get; init; }

    [JsonProperty("projectType")]
    public TimesheetProjectType ProjectType { get; set; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("isEditProject")]
    public bool IsEditProject { get; init; }

    [JsonProperty("date")]
    public string? DateText { get; init; }

    public DateOnly? Date
        =>
        string.IsNullOrEmpty(DateText) ? null : DateOnly.Parse(DateText, CultureInfo.InvariantCulture);

    [JsonProperty("command")]
    public string? Command { get; init; }
}