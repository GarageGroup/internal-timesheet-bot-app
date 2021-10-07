#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed class ImplFlowStateTurnContext<TState> : IFlowStateTurnContext<TState>
    {
        public ImplFlowStateTurnContext(DialogContext dialogContext, TState state)
        {
            DialogContext = dialogContext;
            State = state;
        }

        public TState State { get; }

        public DialogContext DialogContext { get; }

        public BotAdapter Adapter
            =>
            DialogContext.Context.Adapter;

        public TurnContextStateCollection TurnState
            =>
            DialogContext.Context.TurnState;

        public Activity Activity
            =>
            DialogContext.Context.Activity;

        public bool Responded
            =>
            DialogContext.Context.Responded;

        public Task DeleteActivityAsync(string activityId, CancellationToken cancellationToken = default)
            =>
             DialogContext.Context.DeleteActivityAsync(activityId, cancellationToken);

        public Task DeleteActivityAsync(ConversationReference conversationReference, CancellationToken cancellationToken = default)
            =>
            DialogContext.Context.DeleteActivityAsync(conversationReference, cancellationToken);

        public ITurnContext OnDeleteActivity(DeleteActivityHandler handler)
            =>
            DialogContext.Context.OnDeleteActivity(handler);

        public ITurnContext OnSendActivities(SendActivitiesHandler handler)
            =>
            DialogContext.Context.OnSendActivities(handler);

        public ITurnContext OnUpdateActivity(UpdateActivityHandler handler)
            =>
            DialogContext.Context.OnUpdateActivity(handler);

        public Task<ResourceResponse[]> SendActivitiesAsync(IActivity[] activities, CancellationToken cancellationToken = default)
            =>
            DialogContext.Context.SendActivitiesAsync(activities, cancellationToken);

        public Task<ResourceResponse> SendActivityAsync(
            string textReplyToSend, string? speak = null, string inputHint = "acceptingInput", CancellationToken cancellationToken = default)
            =>
            DialogContext.Context.SendActivityAsync(textReplyToSend: textReplyToSend, speak: speak, inputHint: inputHint, cancellationToken);

        public Task<ResourceResponse> SendActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
            =>
            DialogContext.Context.SendActivityAsync(activity, cancellationToken);

        public Task<ResourceResponse> UpdateActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
            =>
            DialogContext.Context.UpdateActivityAsync(activity, cancellationToken);
    }
}