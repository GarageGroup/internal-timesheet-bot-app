using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetDate(
        this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.AwaitDate(
            static _ => new(
                text: "Введите дату списания",
                dateFormat: "dd.MM.yyyy",
                confirmButtonText: "Выбрать",
                invalidDateText: "Не удалось распознать дату",
                DateOnly.FromDateTime(DateTime.Now)),
            static (context, date) => $"Дата: {context.EncodeTextWithStyle(date.ToStringRussianCulture(), BotTextStyle.Bold)}",
            static (state, date) => state with
            {
                Date = date
            });
}