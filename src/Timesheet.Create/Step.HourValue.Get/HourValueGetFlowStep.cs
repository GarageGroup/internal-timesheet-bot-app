using System;
using GGroupp.Infra.Bot.Builder;
using static System.FormattableString;
using static GGroupp.Internal.Timesheet.HourValueGetFlowHelper;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueGetFlowStep
{
    private const int MaxValue = 24;

    internal static ChatFlow<TimesheetCreateFlowStateJson> GetHourValue(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.AwaitValue(
            GetStepOption,
            ParseHourValueOrFailure,
            (state, value) => state with
            {
                ValueHours = value
            });

    private static ValueStepOption GetStepOption(IChatFlowContext<TimesheetCreateFlowStateJson> context)
        =>
        new(
            messageText: "Введите время работы в часах",
            resultText: "Время работы в часах",
            suggestions: GetSuggestions(context));

    private static Result<decimal, BotFlowFailure> ParseHourValueOrFailure(string text)
        =>
        ParseDecimalOrAbsent(text).Fold(ValidateValueOrFailure, CreateUnexpectedValueFailureResult);

    private static Result<decimal, BotFlowFailure> ValidateValueOrFailure(decimal value)
        =>
        value switch
        {
            not > 0 => BotFlowFailure.From("Значение должно быть больше нуля"),
            not <= MaxValue => BotFlowFailure.From(Invariant($"Значение должно быть меньше {MaxValue}")),
            _ => value
        };

    private static Result<decimal, BotFlowFailure> CreateUnexpectedValueFailureResult()
        =>
        BotFlowFailure.From("Не удалось распознать десятичное число");
}