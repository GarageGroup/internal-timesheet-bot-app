using System;
using System.Collections.Generic;
using System.Linq;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Localization;

namespace GarageGroup.Internal.Timesheet;

using static TimesheetCreateResource;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ExpectDateOrSkip(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.SendHtmlModeTextOrSkip(
            BuildSelectedDateText)
        .ExpectDateOrSkip(
            CreateDateStepOption);

    private static string? BuildSelectedDateText(IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Date is null || context.FlowState.ShowSelectedDate is false)
        {
            return null;
        }

        var date = context.FlowState.Date.Value.ToString(DateFormat, context.User.Culture);
        return $"{context.Localizer[SelectedDateName]}: <b>{date}</b>";
    }

    private static DateStepOption<TimesheetCreateFlowState>? CreateDateStepOption(
        IChatFlowContext<TimesheetCreateFlowState> context)
    {
        if (context.FlowState.Date is not null)
        {
            return null;
        }

        return new(
            text: context.Localizer[DateChoiceText],
            forward: context.ValidateDateOrRepeat)
        {
            Suggestions = context.CreateDateSuggestions(),
            InvalidDateText = context.Localizer[InvalidDateText]
        };
    }

    private static FlatArray<FlatArray<KeyValuePair<string, DateOnly>>> CreateDateSuggestions(
        this IChatFlowContext<TimesheetCreateFlowState> context)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var days = DaysInRow * DaysRowsCount;

        return Enumerable.Range(1 - days, days).GroupBy(GetRowNumber).Select(CreateRow).ToFlatArray();

        int GetRowNumber(int index)
            =>
            (index + days - 1) / DaysInRow;

        FlatArray<KeyValuePair<string, DateOnly>> CreateRow<TCollection>(TCollection days)
            where TCollection : IEnumerable<int>
            =>
            days.Select(today.AddDays).Select(CreateSuggestion).ToFlatArray();

        KeyValuePair<string, DateOnly> CreateSuggestion(DateOnly date)
            =>
            (date == today) switch
            {
                true => new(context.Localizer[Today], date),
                _ => new(date.ToString("dd.MM ddd", context.User.Culture).ToUpperInvariant(), date)
            };
    }

    private static Result<TimesheetCreateFlowState, ChatRepeatState> ValidateDateOrRepeat(
        this IChatFlowContext<TimesheetCreateFlowState> context, DateOnly date)
    {
        var today = GetToday();
        if (date > today)
        {
            return ChatRepeatState.From(context.Localizer[FutureDateText]);
        }

        var minDate = new DateOnly(today.Year, today.Month, 1);
        if (today.Day < context.FlowState.LimitationDayOfMonth)
        {
            minDate = new DateOnly(today.Year, today.Month - 1, 1);
        }

        if (date < minDate)
        {
            var text = context.Localizer.GetString(TooEarlyDateTemplate, minDate.ToString(DateFormat, context.User.Culture));
            return ChatRepeatState.From(text);
        }

        return context.FlowState with
        {
            Date = date
        };
    }
}