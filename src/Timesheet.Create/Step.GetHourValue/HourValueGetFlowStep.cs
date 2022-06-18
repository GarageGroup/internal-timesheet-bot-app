using System;
using GGroupp.Infra.Bot.Builder;
using static System.FormattableString;
using static GGroupp.Internal.Timesheet.HourValueGetFlowHelper;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueGetFlowStep
{
    private const int MaxValue = 24;

    internal static ChatFlow<TimesheetCreateFlowState> GetHourValue(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitValue(
            GetStepOption,
            ParseHourValueOrFailure,
            static (context, suggestion) => $"Время работы в часах: {context.EncodeTextWithStyle(suggestion, BotTextStyle.Bold)}",
            (state, value) => state with
            {
                ValueHours = value
            });

    private static ValueStepOption GetStepOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            messageText: "Введите время работы в часах",
            suggestions: GetSuggestions(context));

    private static Result<decimal, BotFlowFailure> ParseHourValueOrFailure(string text)
        =>
        ParseDecimalOrAbsent(text).Fold(ValidateValueOrFailure, CreateUnexpectedValueFailureResult);

    private static Result<decimal, BotFlowFailure> ValidateValueOrFailure(decimal value)
        =>
        value switch
        {
            not > 0 => BotFlowFailure.From("Значение должно быть больше нуля"),
            not <= MaxValue => BotFlowFailure.From(Invariant($"Значение не может быть больше {MaxValue}")),
            _ => value
        };

    private static Result<decimal, BotFlowFailure> CreateUnexpectedValueFailureResult()
        =>
        BotFlowFailure.From("Не удалось распознать десятичное число");
}