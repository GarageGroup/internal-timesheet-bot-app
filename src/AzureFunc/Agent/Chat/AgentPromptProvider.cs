using System;
using System.Globalization;
using System.IO;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentPromptProvider(string template, TimeZoneInfo timeZone, TimeProvider timeProvider)
{
    internal const string RelativePath = "Prompts/AgentSystem.md";

    internal static AgentPromptProvider FromFile(TimeZoneInfo timeZone, TimeProvider timeProvider)
        =>
        new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, RelativePath)), timeZone, timeProvider);

    public string Build(string locale)
    {
        var today = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);

        return template
            .Replace("{{Today}}", today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{DayOfWeek}}", today.DayOfWeek.ToString(), StringComparison.Ordinal)
            .Replace("{{TimeZone}}", timeZone.Id, StringComparison.Ordinal)
            .Replace("{{Locale}}", string.IsNullOrWhiteSpace(locale) ? "ru" : locale, StringComparison.Ordinal);
    }
}
