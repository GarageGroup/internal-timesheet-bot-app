using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet;

internal sealed record class TimesheetSetGetFlowStateJson
{
    [JsonIgnore]
    public DateOnly Date { get; init; }

    [JsonProperty("dateText")]
    public string DateText
    {
        get => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private init => Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}