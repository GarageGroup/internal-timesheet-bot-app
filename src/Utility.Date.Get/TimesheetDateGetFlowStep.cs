using System;
using System.Collections.Generic;
using System.Linq;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using IDateSuggestionsRow = IReadOnlyCollection<KeyValuePair<string, DateOnly>>;

public static class TimesheetDateGetFlowStep
{
    private const int DaysInRow = 3;

    private const string DatePlaceholder = "дд.мм.гг";

    public static ChatFlow<TFlowState> AwaitTimesheetDate<TFlowState>(
        this ChatFlow<TFlowState> chatFlow, string propertyDisplayName, short days, Func<TFlowState, DateOnly, TFlowState> mapFlowState)
    {
        _ = chatFlow ?? throw new ArgumentNullException(nameof(chatFlow));
        _ = mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState));

        return chatFlow.AwaitDate(InnerCreateOptions, GetResultMessage, mapFlowState);

        DateStepOption InnerCreateOptions(IChatFlowContext<TFlowState> context)
            =>
            CreateOptions(context, days);

        string GetResultMessage(IChatFlowContext<TFlowState> context, DateOnly date)
            =>
            propertyDisplayName + ": " + context.EncodeTextWithStyle(date.ToStringRussianCulture(), BotTextStyle.Bold);
    }

    private static DateStepOption CreateOptions<TFlowState>(IChatFlowContext<TFlowState> context, short days)
        =>
        new(
            text: GetDateText(context),
            confirmButtonText: "Выбрать",
            invalidDateText: "Не удалось распознать дату",
            DateOnly.FromDateTime(DateTime.Now),
            placeholder: DatePlaceholder,
            suggestions: context.CreateSuggestions(days));

    private static string GetDateText(ITurnContext context)
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

    private static IReadOnlyCollection<IDateSuggestionsRow> CreateSuggestions(this ITurnContext context, short rows)
    {
        if (context.IsNotTelegramChannel())
        {
            return Array.Empty<IDateSuggestionsRow>();
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var days = DaysInRow * rows;

        return Enumerable.Range(1 - days, days).GroupBy(GetRowNumber).Select(CreateRow).ToArray();

        int GetRowNumber(int index)
            =>
            (index + days - 1) / DaysInRow;

        IDateSuggestionsRow CreateRow<TCollection>(TCollection days)
            where TCollection : IEnumerable<int>
            =>
            days.Select(today.AddDays).Select(CreateSuggestion).ToArray();

        KeyValuePair<string, DateOnly> CreateSuggestion(DateOnly date)
            =>
            date == today ? new("Сегодня", date) : new(date.ToStringRussianCulture("dd.MM ddd"), date);
    }
}