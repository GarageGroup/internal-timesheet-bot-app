using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DateTimesheetFlowStep
{
    private const char HourSymbol = 'ч';

    private const int DaysInRow = 3;

    private const int DaysRowsCount = 3;

    private const string DatePlaceholder = "дд.мм.гг";

    private const string BotLine = "\n\r\n\r";

    private const string TelegramBotLine = "\n\r";

    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator = new('-', 50);

    private static readonly string HeaderLineSeparator = new('_', 43);

    private static readonly CultureInfo RussianCultureInfo;

    static DateTimesheetFlowStep()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    private static string ToDurationStringRussianCulture(this decimal value, bool fixWidth = false)
        =>
        fixWidth
        ? value.ToString("#,##0.00", RussianCultureInfo) + HourSymbol
        : value.ToString("#,##0.##", RussianCultureInfo) + HourSymbol;

    private static string ToStringRussianCulture(this DateOnly date, string format)
        =>
        date.ToString(format, RussianCultureInfo);

    private static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    private static string ToStringRussianCulture(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Возможная сделка",
            TimesheetProjectType.Lead => "Лид",
            TimesheetProjectType.Incident => "Инцидент",
            _ => "Проект"
        };
}