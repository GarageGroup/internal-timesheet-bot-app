using System;
using System.Globalization;

namespace GGroupp.Internal.Timesheet;

public static class TimesheetDateGetUIHelper
{
    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetDateGetUIHelper()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    public static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    internal static string ToStringRussianCulture(this DateOnly date, string format)
        =>
        date.ToString(format, RussianCultureInfo);
}