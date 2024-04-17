using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

internal sealed record WebAppTimesheetsDataJson
{
    public WebAppTimesheetsDataJson(
        [AllowNull] string date,
        [AllowNull] string dateText,
        [AllowNull] IReadOnlyCollection<TimesheetJson> timesheets,
        int allowedDays)
    {
        Date = date.OrEmpty();
        DateText = dateText.OrEmpty();
        Timesheets = timesheets ?? [];
        AllowedDays = allowedDays;
    }

    [JsonProperty("d")]
    public string Date { get; }

    [JsonProperty("dt")]
    public string DateText { get; }

    [JsonProperty("ts")]
    public IReadOnlyCollection<TimesheetJson> Timesheets { get; }

    [JsonProperty("ad")]
    public int AllowedDays { get; }
}