using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DeleteTimesheetFlow
{
    private static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow, 
        ICrmTimesheetApi timesheetApi, 
        DeleteTimesheetOptions options)
        =>
        chatFlow.Start(
            () => new DeleteTimesheetFlowState(options))
        .GetUserId()
        .AwaitDateWebApp()
        .GetTimesheetSet(
            timesheetApi)
        .AwaitTimesheetWebApp()
        .DeleteTimesheet(
            timesheetApi)
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet()
        .MapFlowState(
            Unit.From);
}