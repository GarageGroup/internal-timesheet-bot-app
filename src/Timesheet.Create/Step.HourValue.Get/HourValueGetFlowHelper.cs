using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueGetFlowHelper
{
    private static readonly IReadOnlyCollection<CultureInfo> AwailableCultures;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<string>> TelegramSuggestions;

    private static readonly IReadOnlyCollection<IReadOnlyCollection<string>> TeamsSuggestions;

    static HourValueGetFlowHelper()
    {
        AwailableCultures = new[]
        {
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        };

        TelegramSuggestions = new[]
        {
            new[] { "0,25", "0,5", "0,75", "1" },
            new[] { "1,25", "1,5", "2", "2,5" },
            new[] { "3", "4", "6", "8" }
        };

        TeamsSuggestions = new[]
        {
            new[] { "0,25", "0,5", "0,75", "1", "2", "8" }
        };
    }

    internal static IReadOnlyCollection<IReadOnlyCollection<string>> GetSuggestions(ITurnContext context)
        =>
        context.IsTelegramChannel() ? TelegramSuggestions :
        context.IsMsteamsChannel() ? TeamsSuggestions : Array.Empty<IReadOnlyCollection<string>>();

    internal static Optional<decimal> ParseDecimalOrAbsent(string? text)
        =>
        AwailableCultures.Select(text.ParseWithCultureOrAvsent).FirstOrDefault(r => r.IsPresent);

    private static Optional<decimal> ParseWithCultureOrAvsent(this string? text, CultureInfo culture)
        =>
        decimal.TryParse(text, NumberStyles.Number, culture, out var value) ? Optional.Present(value) : default;
}