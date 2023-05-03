using System;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static partial class TimesheetSetGetChatFlow
{
    private static ChatFlow<Unit> RunFlow(this ChatFlow chatFlow, ConversationState conversationState, ITimesheetSetGetSupplier timesheetApi)
        =>
        chatFlow.Start<DateTimesheetFlowState>(
            static () => new())
        .GetUserId()
        .ReadContextData(
            conversationState)
        .AwaitDate()
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet();
}