using System.Text.Json;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetShowFlowStep
{
    private const int DaysInRow = 3;

    private const int DaysRowsCount = 3;

    private const string BotBreak = "\n\r";

    private const string DateFormat = "d MMMM yyyy";

    private static readonly string HeaderLineSeparator
        =
        new('_', 43);

    private static readonly string LineSeparator
        =
        new('-', 50);

    private static readonly JsonSerializerOptions SerializerOptions
        =
        new(JsonSerializerDefaults.Web);

    private static string GetDisplayName(this IChatFlowContextBase context, TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => context.Localizer["Opportunity"],
            TimesheetProjectType.Lead => context.Localizer["Lead"],
            TimesheetProjectType.Incident => context.Localizer["Incident"],
            _ => context.Localizer["Project"]
        };
}