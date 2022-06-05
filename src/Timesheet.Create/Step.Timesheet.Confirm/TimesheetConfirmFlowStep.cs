using System.Collections.Generic;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetConfirmFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ConfirmCreation(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(
            CreateConfirmationOption);

    private static ConfirmationCardOption CreateConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
        =>
        new(
            questionText: "Списать время?",
            confirmButtonText: "Списать",
            cancelButtonText: "Отменить",
            cancelText: "Списание времени было отменено",
            fieldValues: new KeyValuePair<string, string?>[]
            {
                new(context.FlowState.ProjectType.ToStringRussianCulture(), context.FlowState.ProjectName),
                new("Дата", context.FlowState.Date.ToStringRussianCulture()),
                new("Время", context.FlowState.ValueHours.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Description)
            });
}