using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class UpdateTimesheetFlow
{
    private static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow,
        IBotContext botContext,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi timesheetApi,
        UpdateTimesheetJson? timesheet,
        TimesheetUpdateOption option)
        =>
        chatFlow.Start<TimesheetUpdateFlowState>(
            () => new()
            {
                Date = DateOnly.Parse((timesheet?.Date).OrEmpty()),
                UrlWebApp = option.UrlWebApp
            })
        .GetUserId()
        .GetTimesheetSet(
            timesheetApi)
        .AwaitTimesheetWebApp(
            crmProjectApi)
        .UpdateTimesheet(
            timesheetApi)
        .ShowDateTimesheet(
            botContext);
}