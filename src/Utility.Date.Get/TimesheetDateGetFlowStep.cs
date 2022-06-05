using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

public static class TimesheetDateGetFlowStep
{
    public static ChatFlow<TFlowState> AwaitTimesheetDate<TFlowState>(
        this ChatFlow<TFlowState> chatFlow, string propertyDisplayName, Func<TFlowState, DateOnly, TFlowState> mapFlowState)
    {
        _ = chatFlow ?? throw new ArgumentNullException(nameof(chatFlow));
        _ = mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState));

        return chatFlow.AwaitDate(InnerCreateOptions, GetResultMessage, mapFlowState);

        string GetResultMessage(IChatFlowContext<TFlowState> context, DateOnly date)
            =>
            propertyDisplayName + ": " + context.EncodeTextWithStyle(date.ToStringRussianCulture(), BotTextStyle.Bold);
    }

    private static DateStepOption InnerCreateOptions<TFlowState>(IChatFlowContext<TFlowState> context)
        =>
        new(
            text: GetDateText(context),
            confirmButtonText: "Выбрать",
            invalidDateText: "Не удалось распознать дату",
            DateOnly.FromDateTime(DateTime.Now),
            placeholder: "дд.мм.гг",
            suggestions: context.CreateSuggestions(7));

    private static string GetDateText(ITurnContext context)
    {
        if (context.IsMsteamsChannel())
        {
            return "Выберите дату списания";
        }

        var textBuilder = new StringBuilder("Введите дату списания в формате дд.мм.гг");

        if (context.IsTelegramChannel())
        {
            textBuilder.Append(' ').Append("или выберите день недели");
        }

        return textBuilder.ToString();
    }

    private static IReadOnlyCollection<KeyValuePair<string, DateOnly>> CreateSuggestions(this ITurnContext context, int count)
    {
        if (context.IsNotTelegramChannel())
        {
            return Array.Empty<KeyValuePair<string, DateOnly>>();
        }

        var now = DateOnly.FromDateTime(DateTime.Now);
        return Enumerable.Range(1 - count, count).Select(now.AddDays).Select(CreateSuggestion).ToArray();

        static KeyValuePair<string, DateOnly> CreateSuggestion(DateOnly date)
            =>
            new(date.GetRussianCultureDayOfWeekName(), date);
    }
}