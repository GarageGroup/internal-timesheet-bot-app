using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

internal static partial class DeleteTimesheetFlow
{
    private static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow, 
        ConversationState conversationState, 
        ICrmTimesheetApi timesheetApi, 
        DeleteTimesheetOptions options)
        =>
        chatFlow.Start(
            () => new DeleteTimesheetFlowState(options))
        .GetUserId()
        .ReadContextData(
            conversationState)
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