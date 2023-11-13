using System;
using System.Collections.Generic;
using System.Linq;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class DateTimesheetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> AwaitDate(
        this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.AwaitDate(
            static context => new(
                text: context.GetDateText(),
                confirmButtonText: "Выбрать",
                invalidDateText: "Не удалось распознать дату",
                defaultDate: DateOnly.FromDateTime(DateTime.Now),
                placeholder: DatePlaceholder,
                suggestions: context.CreateSuggestions())
            {
                SkipStep = context.FlowState.Date is not null
            },
            static (context, date) => "Дата: " + context.EncodeTextWithStyle(date.ToStringRussianCulture(), BotTextStyle.Bold),
            static (state, date) => state with
            {
                Date = date
            });

    private static string GetDateText(this ITurnContext context)
    {
        if (context.IsMsteamsChannel())
        {
            return "Выберите дату списания";
        }

        if (context.IsTelegramChannel())
        {
            return $"Выберите или введите дату списания в формате {DatePlaceholder}";
        }

        return $"Введите дату списания в формате {DatePlaceholder}";
    }

    private static IReadOnlyCollection<KeyValuePair<string, DateOnly>>[] CreateSuggestions(this ITurnContext context)
    {
        if (context.IsNotTelegramChannel())
        {
            return [];
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var days = DaysInRow * DaysRowsCount;

        return Enumerable.Range(1 - days, days).GroupBy(GetRowNumber).Select(CreateRow).ToArray();

        int GetRowNumber(int index)
            =>
            (index + days - 1) / DaysInRow;

        IReadOnlyCollection<KeyValuePair<string, DateOnly>> CreateRow<TCollection>(TCollection days)
            where TCollection : IEnumerable<int>
            =>
            days.Select(today.AddDays).Select(CreateSuggestion).ToArray();

        KeyValuePair<string, DateOnly> CreateSuggestion(DateOnly date)
            =>
            date == today ? new("Сегодня", date) : new(date.ToStringRussianCulture("dd.MM ddd").ToUpperInvariant(), date);
    }
}