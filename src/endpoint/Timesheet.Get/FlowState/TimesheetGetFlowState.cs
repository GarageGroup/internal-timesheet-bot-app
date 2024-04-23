using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetGetFlowState
{
    [JsonIgnore]
    public DateOnly? Date { get; init; }

    [JsonProperty("userId")]
    public Guid UserId { get; init; }

    [JsonProperty("dateText")]
    public string? DateText
    {
        get => Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        init
        {
            if (string.IsNullOrEmpty(value))
            {
                Date = null;
            }
            else
            {
                Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
            }
        }
    }

    [JsonProperty("messageText")]
    public string? MessageText { get; init; }

    [JsonProperty("timesheets")]
    public IReadOnlyCollection<TimesheetJson>? Timesheets { get; init; }

    [JsonProperty("urlWebApp")]
    public string? UrlWebApp { get; init; }

    [JsonProperty("limitationDay")]
    public int LimitationDay { get; init; }
}