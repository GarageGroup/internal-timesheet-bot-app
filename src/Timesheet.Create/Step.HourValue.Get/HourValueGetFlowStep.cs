using System;
using System.Globalization;
using GGroupp.Infra.Bot.Builder;
using static System.FormattableString;

namespace GGroupp.Internal.Timesheet;

internal static class HourValueGetFlowStep
{
    private const int MaxValue = 24;

    internal static ChatFlow<TimesheetCreateFlowStateJson> GetHourValue(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.SendText(
            _ => "Введите время работы в часах")
        .AwaitValue(
            text => ParseHourValueOrFailure(text).Forward(ValidateValueOrFailure),
            (state, value) => state with
            {
                ValueHours = value
            });

    private static Result<decimal, Failure<Unit>> ParseHourValueOrFailure(this string? text)
        =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) switch
        {
            true => value,
            _ => Failure.Create("Не удалось распознать десятичное число")
        };

    private static Result<decimal, Failure<Unit>> ValidateValueOrFailure(decimal value)
        =>
        value switch
        {
            <= 0 => Failure.Create("Значение должно быть больше нуля"),
            > MaxValue => Failure.Create(Invariant($"Значение должно быть меньше {MaxValue}")),
            _ => Result.Success(value)
        };
}