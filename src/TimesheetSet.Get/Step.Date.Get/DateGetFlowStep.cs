using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<TimesheetSetGetFlowStateJson> GetDate(
        this ChatFlow<TimesheetSetGetFlowStateJson> chatFlow)
        =>
        chatFlow.AwaitDate(
            static _ => new(
                text: "Введите дату списания",
                dateFormat: "dd.MM.yyyy",
                confirmButtonText: "Выбрать",
                invalidDateText: "Не удалось распознать дату",
                DateOnly.FromDateTime(DateTime.Now)),
            static (state, date) => state with
            {
                Date = date
            });
}