using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteFlowStep
{
    internal static ChatFlow<DeleteTimesheetFlowState> ReadContextData(
        this ChatFlow<DeleteTimesheetFlowState> chatFlow, ConversationState conversationState)
        =>
        chatFlow.Next(
            conversationState.ReadContextDataAsync);

    private static async Task<DeleteTimesheetFlowState> ReadContextDataAsync(
        this ConversationState conversationState, IChatFlowContext<DeleteTimesheetFlowState> context, CancellationToken cancellationToken)
    {
        var contextData = await conversationState.GetContextDataAsync(context, cancellationToken).ConfigureAwait(false);

        if (contextData?.Count is not > 0)
        {
            return context.FlowState;
        }

        return context.FlowState with
        {
            DateText = contextData.GetValueOrDefault("dateText"),
            MessageText = contextData.GetValueOrDefault("messageText")
        };
    }

    private static Task<Dictionary<string, string?>?> GetContextDataAsync(
        this ConversationState conversationState, ITurnContext context, CancellationToken cancellationToken)
        =>
        conversationState.GetContextDataPropertyAccessor().GetAsync(context, default, cancellationToken);

    private static IStatePropertyAccessor<Dictionary<string, string?>?> GetContextDataPropertyAccessor(
        this ConversationState conversationState)
        =>
        conversationState.CreateProperty<Dictionary<string, string?>?>("timesheetData");
}