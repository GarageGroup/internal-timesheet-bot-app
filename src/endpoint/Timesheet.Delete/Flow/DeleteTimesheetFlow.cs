using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DeleteTimesheetFlow
{
    private static ChatFlow<TimesheetDeleteFlowState> RunFlow(
        this ChatFlowStarter<TimesheetDeleteFlowState> chatFlow,
        IBotContext botContext,
        ICrmTimesheetApi timesheetApi,
        WebAppDeleteResponseJson timesheets)
        =>
        chatFlow.Start(
            () => new()
            {
                TimesheetIds = timesheets.Timesheets,
                Date = DateOnly.Parse(timesheets.Date.OrEmpty())
            })
        .DeleteTimesheet(
            timesheetApi)
        .ShowDateTimesheet(
            botContext);
}