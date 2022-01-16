using System;
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
            text => text.ParseHourValueOrFailure().MapFailure(CreateUnexpectedValueFailure).Forward(ValidateValueOrFailure),
            (state, value) => state with
            {
                ValueHours = value
            });

    private static Result<decimal, BotFlowFailure> ValidateValueOrFailure(decimal value)
        =>
        value switch
        {
            not > 0 => BotFlowFailure.From("Значение должно быть больше нуля"),
            not <= MaxValue => BotFlowFailure.From(Invariant($"Значение должно быть меньше {MaxValue}")),
            _ => value
        };

    private static BotFlowFailure CreateUnexpectedValueFailure(Unit _)
        =>
        BotFlowFailure.From("Не удалось распознать десятичное число");
}