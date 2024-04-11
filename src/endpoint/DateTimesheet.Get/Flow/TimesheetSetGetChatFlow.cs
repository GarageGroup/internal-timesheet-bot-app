using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetSetGetChatFlow
{
    private static ChatFlow<DateTimesheetFlowState> RunFlow(
        this ChatFlowStarter<DateTimesheetFlowState> chatFlowStarter,
        ConversationState conversationState,
        ICrmTimesheetApi timesheetApi,
        TimesheetEditOption option)
        =>
        chatFlowStarter.Start(
            () => new()
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