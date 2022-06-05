using System.Globalization;

namespace GGroupp.Internal.Timesheet;

internal static class UITextHelper
{
    private static readonly CultureInfo RussianCultureInfo;

    static UITextHelper()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    internal static string ToStringRussianCulture(this decimal value)
        =>
        value.ToString("G", RussianCultureInfo);

    internal static string ToStringRussianCulture(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Возможная сделка",
            TimesheetProjectType.Lead => "Лид",
            TimesheetProjectType.Incident => "Инцидент",
            _ => "Проект"
        };
}