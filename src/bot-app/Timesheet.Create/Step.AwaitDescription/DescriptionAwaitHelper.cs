using System.Collections.Generic;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DescriptionAwaitHelper
{
    private static readonly ValueStepOption<string> StepOption;

    static DescriptionAwaitHelper()
        =>
        StepOption = new(
            messageText: "Введите описание. Этот шаг можно пропустить",
            suggestions: new[]
        {
            new KeyValuePair<string, string>[] { new("Пропустить", string.Empty) }
        });

    internal static ValueStepOption<string> GetStepOption(IChatFlowContext<TimesheetCreateFlowState> _)
        =>
        StepOption;

    internal static string GetResultMessage(IChatFlowContext<TimesheetCreateFlowState> context, string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return "Описание пропущено";
        }

        return $"Описание: {context.EncodeTextWithStyle(description, BotTextStyle.Bold)}";
    }
}