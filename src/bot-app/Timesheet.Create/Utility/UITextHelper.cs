using System.Globalization;
using System.Text;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

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

    internal static string CreateBoldText(this ITurnContext turnContext, string message)
        =>
        turnContext.IsNotTelegramChannel() switch
        {
            true => new StringBuilder().Append("**").Append(message).Append("**").ToString(),
            _ => new StringBuilder().Append("<b>").Append(message).Append("</b>").ToString()
        };
}