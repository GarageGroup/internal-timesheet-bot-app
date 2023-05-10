using System;
using System.Collections.Generic;
using System.Linq;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

using IDateSuggestionsRow = IReadOnlyCollection<KeyValuePair<string, DateOnly>>;

public static class TimesheetDateGetFlowStep
{
    private const int DaysInRow = 3;

    private const string DatePlaceholder = "дд.мм.гг";

    public static ChatFlow<TFlowState> AwaitTimesheetDate<TFlowState>(
        this ChatFlow<TFlowState> chatFlow,
        Func<IChatFlowContext<TFlowState>, TimesheetDateStepOption> optionFactory,
        Func<TFlowState, DateOnly, TFlowState> mapFlowState)
    {
        ArgumentNullException.ThrowIfNull(chatFlow);
        ArgumentNullException.ThrowIfNull(optionFactory);
        ArgumentNullException.ThrowIfNull(mapFlowState);

        return chatFlow.AwaitDate(InnerCreateOptions, GetResultMessage, mapFlowState);

        DateStepOption InnerCreateOptions(IChatFlowContext<TFlowState> context)
            =>
            CreateOptions(context, optionFactory.Invoke(context));

        string GetResultMessage(IChatFlowContext<TFlowState> context, DateOnly date)
            =>
            optionFactory.Invoke(context).PropertyDisplayName + ": " + context.EncodeTextWithStyle(date.ToStringRussianCulture(), BotTextStyle.Bold);
    }

    private static DateStepOption CreateOptions<TFlowState>(IChatFlowContext<TFlowState> context, TimesheetDateStepOption option)
        =>
        new(
            text: GetDateText(context),
            confirmButtonText: "Выбрать",
            invalidDateText: "Не удалось распознать дату",
            defaultDate: DateOnly.FromDateTime(DateTime.Now),
            placeholder: DatePlaceholder,
            suggestions: context.CreateSuggestions(option.RowsCount))
        {
            SkipStep = option.SkipStep
        };

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
            date == today ? new("Сегодня", date) : new(date.ToStringRussianCulture("dd.MM ddd").ToUpperInvariant(), date);
    }
}