using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct TimesheetCreateCommandIn : IChatCommandIn<Unit>
{
    public static string Type { get; } = "TimesheetCreate";

    public Guid? TimesheetId { get; init; }

    public decimal? Duration { get; init; }

    public TimesheetProjectCommand? Project { get; init; }

    public string? Description { get; init; }

    public DateOnly? Date { get; init; }
}