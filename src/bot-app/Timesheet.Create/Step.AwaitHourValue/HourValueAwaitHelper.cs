using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using static System.FormattableString;

namespace GarageGroup.Internal.Timesheet;

using IHourValueSuggestionCollection = IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>>;

internal static class HourValueAwaitHelper
{
    private const int MaxValue = 24;

    private static readonly IReadOnlyCollection<CultureInfo> AwailableCultures;

    private static readonly IHourValueSuggestionCollection TelegramSuggestions;

    private static readonly IHourValueSuggestionCollection TeamsSuggestions;

    static HourValueAwaitHelper()
    {
        AwailableCultures = new[]
        {
            CultureInfo.GetCultureInfo("ru-RU"),
            CultureInfo.InvariantCulture
        };

        TelegramSuggestions = new[]
        {
            new KeyValuePair<string, decimal>[] { new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1) },
            new KeyValuePair<string, decimal>[] { new("1,25", 1.25m), new("1,5", 1.5m), new("2", 2), new("2,5", 2.5m) },
            new KeyValuePair<string, decimal>[] { new("3", 3), new("4", 4), new("6", 6), new("8", 8) }
        };

        TeamsSuggestions = new[]
        {
            new KeyValuePair<string, decimal>[] { new("0,25", 0.25m), new("0,5", 0.5m), new("0,75", 0.75m), new("1", 1), new("2", 2), new("8", 8) }
        };
    }

    internal static ValueStepOption<decimal> GetStepOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            messageText: "Введите время работы в часах",
            suggestions: GetSuggestions(context));

    internal static Result<decimal, BotFlowFailure> ParseHourValueOrFailure(string text)
        =>
        ParseDecimalOrAbsent(text).Fold(ValidateValueOrFailure, CreateUnexpectedValueFailureResult);

    internal static string GetResultMessage(IChatFlowContext<TimesheetCreateFlowState> context, decimal value)
        =>
        $"Время работы в часах: {context.EncodeTextWithStyle(value.ToStringRussianCulture(), BotTextStyle.Bold)}";

    private static Result<decimal, BotFlowFailure> ValidateValueOrFailure(decimal value)
        =>
        value switch
        {
            not > 0 => BotFlowFailure.From("Значение должно быть больше нуля"),
            not <= MaxValue => BotFlowFailure.From(Invariant($"Значение не может быть больше {MaxValue}")),
            _ => value
        };

    private static IHourValueSuggestionCollection GetSuggestions(ITurnContext context)
    {
        if (context.IsTelegramChannel())
        {
            return TelegramSuggestions;
        }

        if (context.IsMsteamsChannel())
        {
            return TeamsSuggestions;
        }

        return Array.Empty<IReadOnlyCollection<KeyValuePair<string, decimal>>>();
    }

    private static Optional<decimal> ParseDecimalOrAbsent(string? text)
        =>
        AwailableCultures.Select(text.ParseWithCultureOrAbsent).FirstOrDefault(r => r.IsPresent);

    private static Optional<decimal> ParseWithCultureOrAbsent(this string? text, CultureInfo culture)
        =>
        decimal.TryParse(text, NumberStyles.Number, culture, out var value) ? Optional.Present(value) : default;

    private static Result<decimal, BotFlowFailure> CreateUnexpectedValueFailureResult()
        =>
        BotFlowFailure.From("Не удалось распознать десятичное число");
}
