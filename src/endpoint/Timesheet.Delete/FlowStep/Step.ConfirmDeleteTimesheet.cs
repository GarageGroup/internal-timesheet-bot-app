using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> ConfirmDeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(CreateTimesheetConfirmationOption).SetTypingStatus();


    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetDeleteFlowState> context)
        =>
        new(
            questionText: "Удалить списание времени?",
            confirmButtonText: "Удалить",
            cancelButtonText: "Отменить",
            cancelText: "Удаление списания времени было отменено",
            fieldValues:
            [
                new((context.FlowState.Timesheet?.Project?.Type.ToStringRussianCulture()).OrEmpty(), context.FlowState.Timesheet?.Project?.Name),
                new("Дата", context.FlowState.Date.ToStringRussianCulture()),
                new("Время", context.FlowState.Timesheet?.Duration.ToStringRussianCulture() + "ч"),
                new(string.Empty, context.FlowState.Timesheet?.Description)
            ]);
}