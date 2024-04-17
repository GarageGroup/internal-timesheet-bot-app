using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetDeleteFlowStep
{
    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetDeleteFlowStep()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    private static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    private static string ToStringRussianCulture(this decimal value)
        =>
        value.ToString("G", RussianCultureInfo);
}