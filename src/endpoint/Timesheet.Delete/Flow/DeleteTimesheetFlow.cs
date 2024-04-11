using GarageGroup.Infra.Bot.Builder;
using System;
using System.Linq;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DeleteTimesheetFlow
{
    private static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow,
        IBotContext botContext,
        ICrmTimesheetApi timesheetApi,
        WebAppDeleteResponseJson timesheets)
        =>
        chatFlow.Start<TimesheetDeleteFlowState>(
            () => new()
            {
                DeleteTimesheetsId = timesheets.Timesheets,
                Date = DateOnly.Parse(timesheets.Date.OrEmpty())
            })
        .DeleteTimesheet(
            timesheetApi)
        .ShowDateTimesheet(
            botContext);
}