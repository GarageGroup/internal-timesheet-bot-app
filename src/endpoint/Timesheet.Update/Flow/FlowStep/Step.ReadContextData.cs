using GarageGroup.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<UpdateTimesheetFlowState> ReadContextData(
        this ChatFlow<UpdateTimesheetFlowState> chatFlow, ConversationState conversationState)
        =>
        chatFlow.Next(
            conversationState.ReadContextDataAsync);

    private static async Task<UpdateTimesheetFlowState> ReadContextDataAsync(
        this ConversationState conversationState, 
        IChatFlowContext<UpdateTimesheetFlowState> context, 
        CancellationToken cancellationToken)
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
        this ConversationState conversationState, 
        ITurnContext context, 
        CancellationToken cancellationToken)
        =>
        conversationState.GetContextDataPropertyAccessor().GetAsync(context, default, cancellationToken);

    private static IStatePropertyAccessor<Dictionary<string, string?>?> GetContextDataPropertyAccessor(
        this ConversationState conversationState)
        =>
        conversationState.CreateProperty<Dictionary<string, string?>?>("timesheetData");
}