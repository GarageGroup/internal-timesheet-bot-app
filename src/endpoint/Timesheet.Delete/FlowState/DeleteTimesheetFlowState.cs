﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class DeleteTimesheetFlowState
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

    public FlatArray<Guid> DeleteTimesheetsId { get; init; }

    public DeleteTimesheetOptions Options { get; init; }

    public DeleteTimesheetFlowState(DeleteTimesheetOptions options)
    {
        Options = options;
    }
}