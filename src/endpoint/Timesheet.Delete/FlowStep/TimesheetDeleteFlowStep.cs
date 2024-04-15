using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetDeleteFlowStep
{
    private static readonly PipelineParallelOption ParallelOption
        =
        new()
        {
            DegreeOfParallelism = 4
        };

    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetDeleteFlowStep()
    {
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");
    }

    private static string ToStringRussianCulture(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Возможная сделка",
            TimesheetProjectType.Lead => "Лид",
            TimesheetProjectType.Incident => "Инцидент",
            _ => "Проект"
        };

    private static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    private static string ToStringRussianCulture(this decimal value)
        =>
        value.ToString("G", RussianCultureInfo);
}