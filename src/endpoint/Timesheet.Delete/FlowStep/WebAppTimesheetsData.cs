using Newtonsoft.Json;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

internal sealed record WebAppTimesheetsData
{
    [JsonProperty("date")]
    public string? Date { get; init; }

    [JsonProperty("timesheets")]
    public IReadOnlyCollection<TimesheetJson>? Timesheets { get; init; }
}