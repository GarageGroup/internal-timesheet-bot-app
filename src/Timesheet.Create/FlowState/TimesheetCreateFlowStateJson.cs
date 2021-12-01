using System;
using System.Globalization;
using Newtonsoft.Json;

namespace GGroupp.Internal.Timesheet;

internal sealed record class TimesheetCreateFlowStateJson
{
    public Guid UserId { get; init; }

    public Guid ProjectId { get; init; }

    public TimesheetCreateFlowProjectType ProjectType { get; init; }

    public string? ProjectName { get; init; }

    [JsonIgnore]
    public DateOnly Date { get; init; }

    [JsonProperty]
    public string DateText
    {
        get => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private init => Date = DateOnly.Parse(value, CultureInfo.InvariantCulture);
    }

    public decimal ValueHours { get; init; }

    public string? Description { get; init; }
}