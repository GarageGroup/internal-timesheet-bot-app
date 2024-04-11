using System;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetSetGetChatFlow
{
    private static ChatFlow<Unit> RunFlow(this ChatFlow chatFlow, ConversationState conversationState, ICrmTimesheetApi timesheetApi, TimesheetEditOption option)
        =>
        chatFlow.Start(
            () => new DateTimesheetFlowState()
            {
                UrlWebApp = option.UrlWebApp,
                TimesheetInterval = option.TimesheetInterval
            })
        .GetUserId()
        .ReadContextData(
            conversationState)
        .AwaitDate()
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet();
}