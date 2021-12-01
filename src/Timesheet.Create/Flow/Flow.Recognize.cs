using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

partial class TimesheetCreateChatFlow
{
    internal static async ValueTask<Result<ChatFlow, Unit>> InternalRecoginzeOrFailureAsync(
        this IBotContext context, CancellationToken cancellationToken)
    {
        var activity = context.TurnContext.Activity;
        if (activity.IsMessageType() is false)
        {
            return default;
        }

        var chatFlow = ChatFlow.Create(context.TurnContext, context.ConversationState, "TimesheetCreate");
        if (await chatFlow.IsStartedAsync(cancellationToken).ConfigureAwait(false))
        {
            return chatFlow;
        }

        var channelId = activity.ChannelId;
        var text = activity.RemoveRecipientMention().OrEmpty();

        if (channelId is Channels.Telegram)
        {
            if ((text.Length > 1 is false) || (text.StartsWith('/') is false))
            {
                return default;
            }

            text = text.Remove(1);
        }

        if (string.Equals("Списать время", text, StringComparison.InvariantCultureIgnoreCase))
        {
            return chatFlow;
        }

        return default;
    }
}