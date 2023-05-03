using System.Globalization;

namespace GGroupp.Internal.Timesheet;

internal static class UITextHelper
{
    private const char HourSymbol = 'ч';

    private static readonly CultureInfo RussianCultureInfo;

    static UITextHelper()
        =>
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

    internal static string ToDurationStringRussianCulture(this decimal value, bool fixWidth = false)
        =>
        fixWidth
        ? value.ToString("#,##0.00", RussianCultureInfo) + HourSymbol
        : value.ToString("#,##0.##", RussianCultureInfo) + HourSymbol;
}