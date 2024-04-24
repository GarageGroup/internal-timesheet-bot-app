using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetDeleteFlowStep
{
    private static string ToDisplayText(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string ToDisplayText(this decimal value)
        =>
        value.ToString("G", CultureInfo.InvariantCulture);
}