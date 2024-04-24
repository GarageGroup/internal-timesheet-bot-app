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

    private const int MaxValue = 24;

    private const int MaxProjectsCount = 6;

    private const int DescriptionTagDays = 30;

    private const int ProjectDays = 30;

    private const string DatePlaceholder = "dd.mm.yy";

    private const string DefaultProjectMessage
        =
        "Choose a project. Enter part of the name to search";

    private const string ChooseProjectMessage
        =
        "Choose a project or enter part of the name to search";

    private const string NotAllowedFailureUserMessage
        =
        "This operation is not allowed for your account. Please contact the administrator";

    private const string UnexpectedFailureUserMessage
        =
        "An unexpected error occurred. Please contact the administrator or try again later";

    private static readonly IReadOnlyCollection<CultureInfo> AwailableCultures;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>> TelegramSuggestions;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>> TeamsSuggestions;

    static TimesheetCreateFlowStep()
    {
        AwailableCultures =
        [
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        ];

        TelegramSuggestions =
        [
            [new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1)],
            [new("1,25", 1.25m), new("1,5", 1.5m), new("2", 2), new("2,5", 2.5m)],
            [new("3", 3), new("4", 4), new("6", 6), new("8", 8)]
        ];

        TeamsSuggestions =
        [
            [new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1), new("2", 2), new("8", 8)]
        ];
    }

    private static DateOnly GetDateUtc(int daysAddedToNow)
        =>
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysAddedToNow));

    private static DateOnly GetToday()
        =>
        DateOnly.FromDateTime(DateTime.Now);

    private static string ToDisplayText(this DateOnly date, string format)
        =>
        date.ToString(format, CultureInfo.InvariantCulture);

    private static string ToDisplayText(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string ToDisplayText(this decimal value)
        =>
        value.ToString("G", CultureInfo.InvariantCulture);

    private static string ToDisplayText(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Opportunity",
            TimesheetProjectType.Lead => "Lead",
            TimesheetProjectType.Incident => "Incident",
            _ => "Project"
        };

    private static string CreateBoldText(this ITurnContext turnContext, string message)
        =>
        turnContext.IsNotTelegramChannel() ? $"**{message}**" : $"<b>{message}</b>";
}