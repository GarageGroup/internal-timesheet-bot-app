using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DescriptionGetFlowStep
{
    private const string SkipText = "Пропустить";

    internal static ChatFlow<TimesheetCreateFlowState> GetDescription(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitText(
            GetStepOption,
            static (context, suggestion) => $"Описание: {context.EncodeTextWithStyle(suggestion, BotTextStyle.Bold)}",
            BindWithDescription);

    private static ValueStepOption GetStepOption(IChatFlowContext<TimesheetCreateFlowState> _)
        =>
        new(
            messageText: "Введите описание. Этот шаг можно пропустить",
            suggestions: new[]
            {
                new[]
                {
                    SkipText
                }
            });

    private static TimesheetCreateFlowState BindWithDescription(TimesheetCreateFlowState state, string description)
    {
        if (string.Equals(description, SkipText, StringComparison.InvariantCultureIgnoreCase))
        {
            return state;
        }

        return state with
        {
            Description = description
        };
    }
}