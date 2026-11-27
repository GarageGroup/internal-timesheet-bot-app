using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentPromptProvider(IAgentPromptSource source, TimeZoneInfo timeZone, TimeProvider timeProvider)
{
    internal const string RelativePath = "Prompts/AgentSystem.md";

    private const int CalendarDaysBefore = 45;

    private const int CalendarDaysAfter = 14;

    internal AgentPromptProvider(string template, TimeZoneInfo timeZone, TimeProvider timeProvider)
        : this(new StaticAgentPromptSource(template), timeZone, timeProvider)
    {
    }

    // Reads the prompt that is packaged with the application
    internal static string ReadPackagedTemplate()
        =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, RelativePath));

    public async ValueTask<string> BuildAsync(string locale, CancellationToken cancellationToken)
    {
        var template = await source.GetTemplateAsync(cancellationToken).ConfigureAwait(false);
        var today = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);

        return template
            .Replace("{{Today}}", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{DayOfWeek}}", today.DayOfWeek.ToString(), StringComparison.Ordinal)
            .Replace("{{TimeZone}}", timeZone.Id, StringComparison.Ordinal)
            .Replace("{{Locale}}", string.IsNullOrWhiteSpace(locale) ? "ru" : locale, StringComparison.Ordinal)
            .Replace("{{Language}}", GetLanguageName(locale), StringComparison.Ordinal)
            .Replace("{{TodayText}}", AgentDateFormat.Format(DateOnly.FromDateTime(today.Date), locale), StringComparison.Ordinal)
            .Replace("{{Calendar}}", BuildCalendar(DateOnly.FromDateTime(today.Date), locale), StringComparison.Ordinal);
    }

    // Models often miscalculate weekdays, so the prompt lists the dates around today with their ready-to-use text
    private static string BuildCalendar(DateOnly today, string locale)
    {
        var builder = new StringBuilder();

        for (var offset = -CalendarDaysBefore; offset <= CalendarDaysAfter; offset++)
        {
            var date = today.AddDays(offset);
            _ = builder
                .Append(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Append(" = ")
                .Append(AgentDateFormat.Format(date, locale))
                .Append('\n');
        }

        return builder.ToString().TrimEnd();
    }

    private static string GetLanguageName(string locale)
        =>
        locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "английский (English)" : "русский";
}
