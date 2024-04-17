using Newtonsoft.Json;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

internal sealed record WebAppTimesheetsDataJson
{
    [JsonProperty("date")]
    public string? Date { get; init; }

    [JsonProperty("dateText")]
    public string? DateText { get; init; }

    [JsonProperty("timesheets")]
    public IReadOnlyCollection<TimesheetJson>? Timesheets { get; init; }
}