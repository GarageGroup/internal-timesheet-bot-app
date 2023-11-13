using System;
using System.Collections.Generic;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitDescription(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitText(
            static _ => new(
                messageText: "Введите описание. Этот шаг можно пропустить",
                suggestions: new KeyValuePair<string, string>[][]
            {
                [
                    new("Пропустить", string.Empty)
                ]
            }),
            static (context, description) => string.IsNullOrEmpty(description) switch
            {
                true => "Описание пропущено",
                _ => $"Описание: {context.EncodeTextWithStyle(description, BotTextStyle.Bold)}"
            },
            static (flowState, description) => flowState with
            {
                Description = description.OrNullIfEmpty()
            });
}