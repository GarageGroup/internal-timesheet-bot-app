using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal readonly record struct WebAppCreateTimesheetDataJson
{
    [JsonProperty("duration")]
    public decimal? Duration { get; init; }

    [JsonProperty("project")]
    public TimesheetProjectState? Project { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("date")]
    public string? Date { get; init; }
}