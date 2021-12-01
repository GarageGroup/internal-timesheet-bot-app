using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Infra.Bot.Builder;

internal sealed class ChatFlowContextImpl<TFlowState> : IChatFlowContext<TFlowState>
{
    private readonly ITurnContext sourceContext;

    internal ChatFlowContextImpl(ITurnContext sourceContext, TFlowState flowState, object? stepState)
    {
        this.sourceContext = sourceContext;
        FlowState = flowState;
        StepState = stepState;
    }

    IChatFlowContext<TResult> IChatFlowContext<TFlowState>.InternalMapFlowState<TResult>(Func<TFlowState, TResult> map)
        =>
        new ChatFlowContextImpl<TResult>(sourceContext, map.Invoke(FlowState), StepState);

    public TFlowState FlowState { get; }

    public object? StepState { get; }

    public BotAdapter Adapter
        =>
        sourceContext.Adapter;

    public TurnContextStateCollection TurnState
        =>
        sourceContext.TurnState;

    public Activity Activity
        =>
        sourceContext.Activity;

    public bool Responded
        =>
        sourceContext.Responded;

    public Task DeleteActivityAsync(string activityId, CancellationToken cancellationToken = default)
        =>
        sourceContext.DeleteActivityAsync(activityId, cancellationToken);

    public Task DeleteActivityAsync(ConversationReference conversationReference, CancellationToken cancellationToken = default)
        =>
        sourceContext.DeleteActivityAsync(conversationReference, cancellationToken);

    public ITurnContext OnDeleteActivity(DeleteActivityHandler handler)
        =>
        sourceContext.OnDeleteActivity(handler);

    public ITurnContext OnSendActivities(SendActivitiesHandler handler)
        =>
        sourceContext.OnSendActivities(handler);

    public ITurnContext OnUpdateActivity(UpdateActivityHandler handler)
        =>
        sourceContext.OnUpdateActivity(handler);

    public Task<ResourceResponse[]> SendActivitiesAsync(IActivity[] activities, CancellationToken cancellationToken = default)
        =>
        sourceContext.SendActivitiesAsync(activities, cancellationToken);

    public Task<ResourceResponse> SendActivityAsync(string textReplyToSend, string? speak = null, string inputHint = "acceptingInput", CancellationToken cancellationToken = default)
        =>
        sourceContext.SendActivityAsync(textReplyToSend, speak, inputHint, cancellationToken);

    public Task<ResourceResponse> SendActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
        =>
        sourceContext.SendActivityAsync(activity, cancellationToken);

    public Task<ResourceResponse> UpdateActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
        =>
        sourceContext.UpdateActivityAsync(activity, cancellationToken);
}