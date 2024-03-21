using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetDeleteFlowStep
{
    private const char HourSymbol = 'ч';

    private const string DatePlaceholder = "дд.мм.гг";

    private const string BotLine = "\n\r\n\r";

    private const string TelegramBotLine = "\n\r";

    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator = new('-', 50);

    private static readonly string HeaderLineSeparator = new('_', 43);

    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetDeleteFlowStep()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    private static string ToDurationStringRussianCulture(this decimal value, bool fixWidth = false)
        =>
        fixWidth
        ? value.ToString("#,##0.00", RussianCultureInfo) + HourSymbol
        : value.ToString("#,##0.##", RussianCultureInfo) + HourSymbol;

    private static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);
}