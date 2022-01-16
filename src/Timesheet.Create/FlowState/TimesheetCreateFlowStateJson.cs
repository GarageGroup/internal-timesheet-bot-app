using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet;

internal sealed record class TimesheetCreateFlowStateJson
{
    [JsonProperty("projectId")]
    public Guid ProjectId { get; init; }

    [JsonProperty("projectType")]
    public TimesheetProjectType ProjectType { get; init; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

    [JsonIgnore]
    public DateOnly Date { get; init; }

    [JsonProperty("dateText")]
    public string DateText
    {
        get => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private init => Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    [JsonProperty("valueHours")]
    public decimal ValueHours { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }
}