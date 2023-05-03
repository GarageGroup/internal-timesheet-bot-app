using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class ContextDataReadFlowState
{
    internal static ChatFlow<DateTimesheetFlowState> ReadContextData(
        this ChatFlow<DateTimesheetFlowState> chatFlow, ConversationState conversationState)
        =>
        chatFlow.Next(
            conversationState.ReadContextDataAsync);
}