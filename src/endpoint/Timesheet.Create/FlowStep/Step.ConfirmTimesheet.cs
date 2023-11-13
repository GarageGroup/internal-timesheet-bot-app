using System;
using System.Collections.Generic;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> ConfirmTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(CreateTimesheetConfirmationOption).SetTypingStatus();

    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetCreateFlowState> context)
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