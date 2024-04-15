using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DeleteTimesheetFlow
{
    private static ChatFlow<TimesheetDeleteFlowState> RunFlow(
        this ChatFlowStarter<TimesheetDeleteFlowState> chatFlow,
        IBotContext botContext,
        ICrmTimesheetApi timesheetApi,
        WebAppDeleteResponseJson? timesheet)
        =>
        chatFlow.Start(
            () => new()
            {
                Timesheet = timesheet?.Timesheet,
                Date = DateOnly.Parse((timesheet?.Date).OrEmpty())
            })
        .ConfirmDeleteTimesheet()
        .DeleteTimesheet(
            timesheetApi)
        .ShowDateTimesheet(
            botContext);
}