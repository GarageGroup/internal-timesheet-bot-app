using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppTimesheetsDataJson
{
    public WebAppTimesheetsDataJson(
        [AllowNull] string date,
        [AllowNull] string dateText,
        [AllowNull] FlatArray<TimesheetJson> timesheets,
        int allowedDays)
    {
        Date = date.OrEmpty();
        DateText = dateText.OrEmpty();
        Timesheets = timesheets;
        AllowedDays = allowedDays;
    }

    [JsonPropertyName("d")]
    public string Date { get; }

    [JsonPropertyName("dt")]
    public string DateText { get; }

    [JsonPropertyName("ts")]
    public FlatArray<TimesheetJson> Timesheets { get; }

    [JsonPropertyName("ad")]
    public int AllowedDays { get; }
}