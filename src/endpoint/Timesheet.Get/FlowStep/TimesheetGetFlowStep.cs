using System;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetGetFlowStep
{
    private const char HourSymbol = 'h';

    private const int DaysInRow = 3;

    private const int DaysRowsCount = 3;

    private const string DatePlaceholder = "dd.mm.yy";

    private const string BotLine = "\n\r\n\r";

    private const string TelegramBotLine = "\n\r";

    private const string TimeColumnWidth = "45px";

    private const string NotAllowedFailureUserMessage
        =
        "This operation is not allowed for your account. Please contact the administrator";

    private const string UnexpectedFailureUserMessage
        =
        "An unexpected error occurred. Please contact the administrator or try again later";

    private static readonly string LineSeparator = new('-', 50);

    private static readonly string HeaderLineSeparator = new('_', 43);

    private static string ToDurationDisplayText(this decimal value, bool fixWidth = false)
        =>
        value.ToString(fixWidth ? "#,##0.00" : "#,##0.##", CultureInfo.InvariantCulture) + HourSymbol;

    private static string ToDisplayText(this DateOnly date, string format)
        =>
        date.ToString(format, CultureInfo.InvariantCulture);

    private static string ToDisplayText(this DateOnly date)
        =>
        date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string ToDisplayText(this TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Opportunity => "Opportunity",
            TimesheetProjectType.Lead => "Lead",
            TimesheetProjectType.Incident => "Incident",
            _ => "Project"
        };
}