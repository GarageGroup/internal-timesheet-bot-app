using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

public readonly record struct TimesheetShowCommandIn : IChatCommandIn<Unit>
{
    public static string Type { get; } = "TimesheetShow";

    public DateOnly? Date { get; init; }

    public string? MessageText { get; init; }
}