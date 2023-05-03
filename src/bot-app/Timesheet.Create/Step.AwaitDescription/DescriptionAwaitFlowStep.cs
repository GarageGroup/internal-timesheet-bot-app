using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DescriptionAwaitFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitDescription(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitText(
            DescriptionAwaitHelper.GetStepOption,
            DescriptionAwaitHelper.GetResultMessage,
            static (flowState, description) => flowState with
            {
                Description = description.OrNullIfEmpty()
            });
}