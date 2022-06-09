using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet;

internal sealed record class DateTimesheetFlowState
{
    [JsonIgnore]
    public DateOnly Date { get; init; }

    [JsonProperty("userId")]
    public Guid UserId { get; init; }

    [JsonProperty("dateText")]
    public string DateText
    {
        get => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private init => Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    [JsonProperty("timesheets")]
    public IReadOnlyCollection<TimesheetJson>? Timesheets { get; init; }
}