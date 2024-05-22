using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetDeleteCommandIn : IChatCommandIn<Unit>
{
    public static string Type { get; } = "TimesheetDelete";

    public required Guid TimesheetId { get; init; }

    public required DateOnly Date { get; init; }

    public required string? ProjectName { get; init; }

    public required string? ProjectTypeDisplayName { get; init; }

    public required decimal Duration { get; init; }

    public required string? Description { get; init; }
}