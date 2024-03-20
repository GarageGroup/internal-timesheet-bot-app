using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class UpdateTimesheetFlow
{
    private static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi timesheetApi, 
        UpdateTimesheetOptions options)
        =>
        chatFlow.Start(
            () => new UpdateTimesheetFlowState(options))
        .GetUserId()
        .AwaitDateWebApp()
        .GetTimesheetSet(
            timesheetApi)
        .AwaitTimesheetWebApp(
            crmProjectApi)
        .UpdateTimesheet(
            timesheetApi)
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet()
        .MapFlowState(
            Unit.From);
}