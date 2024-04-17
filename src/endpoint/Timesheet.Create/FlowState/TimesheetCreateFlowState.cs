using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetCreateFlowState
{
    [JsonProperty("timesheetId")]
    public Guid? TimesheetId { get; set; }

    [JsonProperty("userId")]
    public Guid? UserId { get; init; }

    [JsonProperty("project")]
    public TimesheetProjectState? Project { get; init; }

    [JsonIgnore]
    public DateOnly? Date { get; init; }

    [JsonProperty("dateText")]
    public string? DateText
    {
        get => Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        init => Date = value is null ? null : DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    [JsonProperty("valueHours")]
    public decimal? ValueHours { get; init; }

    [JsonProperty("descriptionTags")]
    public string[]? DescriptionTags { get; init; }

    [JsonProperty("description")]
    public TimesheetDescriptionState? Description { get; init; }

    [JsonProperty("allowedIntervalInDays")]
    public int AllowedIntervalInDays { get; init; }

    [JsonProperty("urlWebApp")]
    public string? UrlWebApp { get; init; }

    [JsonProperty("withoutConfirmation")]
    public bool WithoutConfirmation { get; set; }
}