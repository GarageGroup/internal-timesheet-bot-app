using Flow.FlowStep;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetSetDeleteChatFlow
{
    private static ChatFlow<Unit> RunFlow(this ChatFlow chatFlow, ConversationState conversationState, ICrmTimesheetApi timesheetApi, DeleteTimesheetOptions options)
        =>
        chatFlow.Start<DeleteTimesheetFlowState>(
            () => new() 
            { 
                Options = options 
            })
        .GetUserId()
        .ReadContextData(
            conversationState)
        .AwaitDateWebApp()
        .GetTimesheetSet(
            timesheetApi)
        .AwaitTimesheetWebApp()
        .DeleteTimesheet(timesheetApi)
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet()
        .MapFlowState(Unit.From);
}
