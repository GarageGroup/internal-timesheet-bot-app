﻿using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetSetGetChatFlow
{
    private static ChatFlow<DateTimesheetFlowState> RunFlow(
        this ChatFlowStarter<DateTimesheetFlowState> chatFlowStarter,
        ConversationState conversationState,
        ICrmTimesheetApi timesheetApi,
        DateTimesheetEditOption option)
        =>
        chatFlowStarter.Start(
            () => new()
            {
                UrlWebApp = option.UrlWebApp,
                AllowedIntervalInDays = option.AllowedIntervalInDays
            })
        .GetUserId()
        .ReadContextData(
            conversationState)
        .AwaitDate()
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet();
}