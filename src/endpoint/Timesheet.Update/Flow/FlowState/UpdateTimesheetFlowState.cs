using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class UpdateTimesheetFlowState
{
    [JsonIgnore]
    public DateOnly? Date { get; init; }

    [JsonProperty("userId")]
    public Guid UserId { get; init; }

    [JsonProperty("projectId")]
    public Guid ProjectId { get; init; }

    [JsonProperty("projectType")]
    public TimesheetProjectType ProjectType { get; init; }

    [JsonProperty("projectName")]
    public string? ProjectName { get; init; }

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

    public UpdateTimesheetJson? TimesheetUpdate { get; init; }

    public bool UpdateProject { get; init; }

    public UpdateTimesheetOptions Options { get; init; }

    public UpdateTimesheetFlowState(UpdateTimesheetOptions options)
    {
        Options = options;
    }
}