using Newtonsoft.Json;
using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetDeleteFlowState
{
    [JsonIgnore]
    public DateOnly Date { get; init; }

    [JsonIgnore]
    public FlatArray<Guid> TimesheetIds { get; init; }

    [JsonProperty("dateText")]
    public string DateText
    {
        get => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private init => Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }
}