using System;

namespace GGroupp.Internal.Timesheet;

internal sealed record class TimesheetCreateFlowResultJson
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }
}