using System;
using System.Collections.Generic;
using System.Globalization;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateFlowStep
{
    private const int DaysInRow = 3;

    private const int DaysRowsCount = 2;

    private const string DatePlaceholder = "дд.мм.гг";

    private const int MaxValue = 24;

    private const string DefaultProjectMessage = "Нужно выбрать проект. Введите часть названия для поиска";

    private const string ChooseProjectMessage = "Выберите проект или введите часть названия для поиска";

    private const int MaxProjectsCount = 6;

    private static readonly IReadOnlyCollection<CultureInfo> AwailableCultures;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>> TelegramSuggestions;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>> TeamsSuggestions;

    private static readonly CultureInfo RussianCultureInfo;

    static TimesheetCreateFlowStep()
    {
        RussianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

        AwailableCultures = new[]
        {
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        };

        TelegramSuggestions = new KeyValuePair<string, decimal>[][]
        {
            [new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1)],
            [new("1,25", 1.25m), new("1,5", 1.5m), new("2", 2), new("2,5", 2.5m)],
            [new("3", 3), new("4", 4), new("6", 6), new("8", 8)]
        };

        TeamsSuggestions = new KeyValuePair<string, decimal>[][]
        {
            [new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1), new("2", 2), new("8", 8)]
        };
    }

    private static string ToStringRussianCulture(this DateOnly date, string format)
        =>
        date.ToString(format, RussianCultureInfo);

    private static string ToStringRussianCulture(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", RussianCultureInfo);

    private static string ToStringRussianCulture(this decimal value)
        =>
        value.ToString("G", RussianCultureInfo);

    private static string ToStringRussianCulture(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Возможная сделка",
            TimesheetProjectType.Lead => "Лид",
            TimesheetProjectType.Incident => "Инцидент",
            _ => "Проект"
        };

    private static string CreateBoldText(this ITurnContext turnContext, string message)
        =>
        turnContext.IsNotTelegramChannel() ? $"**{message}**" : $"<b>{message}</b>";
}