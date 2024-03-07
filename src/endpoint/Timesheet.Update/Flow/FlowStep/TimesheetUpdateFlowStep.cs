using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetUpdateFlowStep
{
    private const char HourSymbol = 'ч';

    private const string DatePlaceholder = "дд.мм.гг";

    private const string BotLine = "\n\r\n\r";

    private const string TelegramBotLine = "\n\r";

    private const string TimeColumnWidth = "45px";

    private static readonly string LineSeparator = new('-', 50);

    private static readonly string HeaderLineSeparator = new('_', 43);

    private const int MaxProjectsCount = 6;

    private const int ProjectDays = 30;

    private const string DefaultProjectMessage = "Нужно выбрать проект. Введите часть названия для поиска";

    private const string ChooseProjectMessage = "Выберите проект или введите часть названия для поиска";

    private const string UnknownErrorText = "Возникла непредвиденная ошибка. Обратитесь к администратору.";

    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetUpdateFlowStep()
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

    private static DateOnly GetDateUtc(int daysAddedToNow)
        =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAddedToNow));
}