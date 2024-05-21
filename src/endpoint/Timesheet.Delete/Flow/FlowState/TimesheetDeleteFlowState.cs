using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetDeleteFlowState
{
    public required Guid TimesheetId { get; init; }

    public DateOnly Date { get; init; }

    public decimal Duration { get; init; }

    public string? Description { get; init; }

    public string? ProjectName { get; init; }

    public string? ProjectTypeDisplayName { get; init; }
}