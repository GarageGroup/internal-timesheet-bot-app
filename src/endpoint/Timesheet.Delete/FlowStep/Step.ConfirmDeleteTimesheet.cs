using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<TimesheetDeleteFlowState> ConfirmDeleteTimesheet(
        this ChatFlow<TimesheetDeleteFlowState> chatFlow)
        =>
        chatFlow.AwaitConfirmation(CreateTimesheetConfirmationOption);


    private static ConfirmationCardOption CreateTimesheetConfirmationOption(IChatFlowContext<TimesheetDeleteFlowState> context)
        =>
        new(
            entity: new(
                headerText: "Delete timesheet?",
                fieldValues:
                [
                    new((context.FlowState.Timesheet?.Project?.DisplayTypeName).OrEmpty(), context.FlowState.Timesheet?.Project?.Name),
                    new("Date", context.FlowState.Date.ToDisplayText()),
                    new("Duration", context.FlowState.Timesheet?.Duration.ToDisplayText() + "h"),
                    new(string.Empty, context.FlowState.Timesheet?.Description)
                ]),
            buttons: new(
                confirmButtonText: "Delete",
                cancelButtonText: "Cancel",
                cancelText: "The deletion of the timesheet was canceled"));
}