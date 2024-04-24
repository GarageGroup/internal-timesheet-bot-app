using Newtonsoft.Json;
using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppDataTimesheetUpdateJson
{
    [JsonProperty("id")]
    public Guid Id { get; init; }

    [JsonProperty("duration")]
    public decimal Duration { get; init; }

    [JsonProperty("project")]
    public TimesheetProjectState? Project { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("date")]
    public string? DateText { get; init; }

    [JsonIgnore]
    public DateOnly? Date
        =>
        string.IsNullOrEmpty(DateText) ? null : DateOnly.Parse(DateText, CultureInfo.InvariantCulture);

    [JsonProperty("command")]
    public string? Command { get; init; }
}