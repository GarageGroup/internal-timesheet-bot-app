using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

// Dates are shown to the user as "27 ноября 2026, пятница" or "27 November 2026, Friday"
internal static class AgentDateFormat
{
    private const string Pattern = "d MMMM yyyy, dddd";

    // A day inside a period report: the year is in the period heading
    private const string DayPattern = "d MMMM, dddd";

    private const string PeriodDatePattern = "dd.MM.yyyy";

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    internal static CultureInfo GetCulture(string? locale)
        =>
        locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? English : Russian;

    internal static string Format(DateOnly date, string? locale)
        =>
        date.ToString(Pattern, GetCulture(locale));

    internal static string FormatDay(DateOnly date, string? locale)
        =>
        date.ToString(DayPattern, GetCulture(locale));

    // A period is shown the same way in every language: "23.11.2026 – 29.11.2026"
    internal static string FormatPeriod(DateOnly from, DateOnly to)
        =>
        from.ToString(PeriodDatePattern, CultureInfo.InvariantCulture) + " – " + to.ToString(PeriodDatePattern, CultureInfo.InvariantCulture);
}
