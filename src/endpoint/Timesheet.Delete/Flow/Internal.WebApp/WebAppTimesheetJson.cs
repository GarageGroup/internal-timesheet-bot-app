using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class WebAppTimesheetJson
{
    public Guid Id { get; init; }

    public decimal Duration { get; init; }

    public WebAppProjectJson? Project { get; init; }

    public string? Description { get; init; }
}