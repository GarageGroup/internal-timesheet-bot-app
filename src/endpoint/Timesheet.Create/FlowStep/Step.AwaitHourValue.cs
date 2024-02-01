using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using static System.FormattableString;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitHourValue(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitValue(
            static context => new(
                messageText: "Введите время работы в часах",
                suggestions: context.GetHourValueSuggestions()),
            static text => ParseDecimalOrAbsent(text).Fold(ValidateHourValueOrFailure, CreateUnexpectedValueFailureResult),
            static (context, value) => $"Время работы в часах: {context.EncodeTextWithStyle(value.ToStringRussianCulture(), BotTextStyle.Bold)}",
            (state, value) => state with
            {
                ValueHours = value
            });

    private static IReadOnlyCollection<IReadOnlyCollection<KeyValuePair<string, decimal>>> GetHourValueSuggestions(this ITurnContext context)
    {
        if (context.IsTelegramChannel())
        {
            return TelegramSuggestions;
        }

        if (context.IsMsteamsChannel())
        {
            return TeamsSuggestions;
        }

        return [];
    }

    private static Result<decimal, BotFlowFailure> ValidateHourValueOrFailure(decimal value)
        =>
        value switch
        {
            not > 0 => BotFlowFailure.From("Значение должно быть больше нуля"),
            not <= MaxValue => BotFlowFailure.From(Invariant($"Значение не может быть больше {MaxValue}")),
            _ => value
        };

    private static Optional<decimal> ParseDecimalOrAbsent(string? text)
        =>
        AwailableCultures.Select(text.ParseWithCultureOrAbsent).FirstOrDefault(static r => r.IsPresent);

    private static Optional<decimal> ParseWithCultureOrAbsent(this string? text, CultureInfo culture)
        =>
        decimal.TryParse(text, NumberStyles.Number, culture, out var value) ? Optional.Present(value) : default;

    private static Result<decimal, BotFlowFailure> CreateUnexpectedValueFailureResult()
        =>
        BotFlowFailure.From("Не удалось распознать десятичное число");
}