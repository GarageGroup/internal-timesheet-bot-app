using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppTimesheetUpdateData
{
    public Guid? Id { get; init; }

    public decimal? Duration { get; init; }

    public TimesheetProjectState? Project { get; init; }

    public string? Description { get; init; }

    public DateOnly? Date { get; init; }

    public string? Command { get; init; }
}