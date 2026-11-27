using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

// Dates are shown to the user as "27 ноября 2026, пятница" or "27 November 2026, Friday"
internal static class AgentDateFormat
{
    private const string Pattern = "d MMMM yyyy, dddd";

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");

    internal static CultureInfo GetCulture(string? locale)
        =>
        locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) is true ? English : Russian;

    internal static string Format(DateOnly date, string? locale)
        =>
        date.ToString(Pattern, GetCulture(locale));
}
