using System;
using System.Collections.Generic;
using System.Linq;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitDate(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitDate(
            static context => new(
                text: context.GetDateText(),
                confirmButtonText: "Choose",
                invalidDateText: "Failed to recognize the date",
                defaultDate: GetToday(),
                placeholder: DatePlaceholder,
                suggestions: context.CreateSuggestions())
            {
                SkipStep = context.FlowState.Date is not null
            },
            static (context, date) => "Date: " + context.EncodeTextWithStyle(date.ToDisplayText(), BotTextStyle.Bold),
            ValidateDateOrFailure,
            static (state, date) => state with
            {
                Date = date
            });

    private static string GetDateText(this ITurnContext context)
    {
        if (context.IsMsteamsChannel())
        {
            return "Choose the date";
        }

        if (context.IsTelegramChannel())
        {
            return $"Choose or enter the date in the format {DatePlaceholder}";
        }

        return $"Enter the date in the format {DatePlaceholder}";
    }

    private static IReadOnlyCollection<KeyValuePair<string, DateOnly>>[] CreateSuggestions(this ITurnContext context)
    {
        if (context.IsNotTelegramChannel())
        {
            return [];
        }

        var today = GetToday();
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
            date == today ? new("Today", date) : new(date.ToDisplayText("dd.MM ddd").ToUpperInvariant(), date);
    }

    private static Result<DateOnly, BotFlowFailure> ValidateDateOrFailure(
        this IChatFlowContext<TimesheetCreateFlowState> context, DateOnly date)
    {
        var today = GetToday();
        if (date > today)
        {
            return BotFlowFailure.From("You cannot specify a future date");
        }

        var minDate = new DateOnly(today.Year, today.Month, 1);
        if (today.Day < context.FlowState.LimitationDay)
        {
            minDate = new DateOnly(today.Year, today.Month - 1, 1);
        }

        if (date < minDate)
        {
            return BotFlowFailure.From($"You cannot choose a date earlier than {minDate.ToDisplayText()}");
        }

        return Result.Success(date);
    }
}