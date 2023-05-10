using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class ContextDataReadFlowState
{
    internal static ChatFlow<DateTimesheetFlowState> ReadContextData(
        this ChatFlow<DateTimesheetFlowState> chatFlow, ConversationState conversationState)
        =>
        chatFlow.Next(
            conversationState.ReadContextDataAsync);
}