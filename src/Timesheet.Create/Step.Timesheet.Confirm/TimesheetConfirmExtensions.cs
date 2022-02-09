using System;
using System.Globalization;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmExtensions
{
    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetConfirmExtensions()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    internal static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    internal static string ToStringRussianCulture(this decimal value)
        =>
        value.ToString("G", RussianCultureInfo);
}