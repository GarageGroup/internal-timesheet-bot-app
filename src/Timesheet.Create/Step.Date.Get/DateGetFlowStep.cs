using System;
using System.Globalization;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowStateJson> GetDate(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.SendText(
            _ => "Введите дату в формате дд.мм.гггг")
        .AwaitValue(
            ParseDateOrFailure,
            (state, date) => state with
            {
                Date = date
            });

    private static Result<DateOnly, ChatFlowStepFailure> ParseDateOrFailure(string? text)
        =>
        DateOnly.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) switch
        {
            true => date,
            _ => ChatFlowStepFailure.FromUI("Не удалось распознать дату")
        };
}