using System;
using System.Collections.Generic;
using System.Linq;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetShowFlowStep
{
    internal static ChatFlow<TimesheetShowFlowState> ExpectDate(
        this ChatFlow<TimesheetShowFlowState> chatFlow)
        =>
        chatFlow.ExpectDateOrSkip(
            GetDateStepOption);

    private static DateStepOption<TimesheetShowFlowState>? GetDateStepOption(IChatFlowContext<TimesheetShowFlowState> context)
        =>
        context.FlowState.Date is not null ? null : new(
            text: context.Localizer[TimesheetShowResource.DateChoiceText],
            forward: date => context.FlowState with
            {
                Date = date
            })
        {
            Suggestions = context.CreateDateSuggestions(),
            InvalidDateText = context.Localizer[TimesheetShowResource.InvalidDateText]
        };

    private static FlatArray<FlatArray<KeyValuePair<string, DateOnly>>> CreateDateSuggestions(
        this IChatFlowContext<TimesheetShowFlowState> context)
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
                true => new(context.Localizer[TimesheetShowResource.Today], date),
                _ => new(date.ToString("dd.MM ddd", context.User.Culture).ToUpperInvariant(), date)
            };
    }
}