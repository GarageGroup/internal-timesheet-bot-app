using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetBeforeUpdateJson
{
    [JsonProperty("duration")]
    public decimal Duration { get; init; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }
}