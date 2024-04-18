using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetGetChatFlow
{
    private static ChatFlow<TimesheetGetFlowState> RunFlow(
        this ChatFlowStarter<TimesheetGetFlowState> chatFlowStarter,
        ConversationState conversationState,
        ICrmTimesheetApi timesheetApi,
        TimesheetGetFlowOption option)
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