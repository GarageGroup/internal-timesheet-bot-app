using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentPromptProvider(IAgentPromptSource source, TimeZoneInfo timeZone, TimeProvider timeProvider)
{
    internal const string RelativePath = "Prompts/AgentSystem.md";

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
            .Replace("{{Language}}", GetLanguageName(locale), StringComparison.Ordinal);
    }

    private static string GetLanguageName(string locale)
        =>
        locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "английский (English)" : "русский";
}
