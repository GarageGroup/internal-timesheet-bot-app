using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DescriptionGetFlowStep
{
    private const string SkipText = "Пропустить";

    internal static ChatFlow<TimesheetCreateFlowStateJson> GetDescription(
        this ChatFlow<TimesheetCreateFlowStateJson> chatFlow)
        =>
        chatFlow.AwaitText(
            GetStepOption,
            static (context, suggestion) => $"Описание: {context.EncodeTextWithStyle(suggestion, BotTextStyle.Bold)}",
            BindWithDescription);

    private static ValueStepOption GetStepOption(IChatFlowContext<TimesheetCreateFlowStateJson> _)
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

    private static TimesheetCreateFlowStateJson BindWithDescription(TimesheetCreateFlowStateJson state, string description)
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