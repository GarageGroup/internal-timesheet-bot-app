using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowStateJson> GetDate(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.AwaitDate(
            _ => GetAwaitDateOption(),
            (state, date) => state with
            {
                Date = date
            });

    private static AwaitDateOption GetAwaitDateOption()
        =>
        new(
            text: "Введите дату списания",
            dateFormat: "dd.MM.yyyy",
            confirmButtonText: "Выбрать",
            invalidDateText: "Не удалось распознать дату",
            DateOnly.FromDateTime(DateTime.Now));
}