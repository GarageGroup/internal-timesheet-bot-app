using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    public Guid Id { get; init; }

    public decimal Duration { get; init; }

    public ProjectJson? Project { get; init; }

    public string? Description { get; init; }

    public StateCode? IncidentStateCode { get; init; }

    public StateCode TimesheetStateCode { get; init; }
}