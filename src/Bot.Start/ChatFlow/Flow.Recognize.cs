using System;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

partial class BotStartChatFlow
{
    internal static Optional<IBotContext> RecognizeCommandOrAbsent(this IBotContext context)
    {
        if (context.TurnContext.Activity.IsMessageType() is false)
        {
            return default;
        }

        var channelId = context.TurnContext.Activity.ChannelId;
        var text = context.TurnContext.Activity.RemoveRecipientMention().OrEmpty();

        if (channelId is Channels.Telegram)
        {
            if ((text.Length > 1 is false) || (text.StartsWith('/') is false))
            {
                return default;
            }

            text = text.Remove(1);
        }
        else if (channelId is Channels.Msteams)
        {
            if (IsCommand("<a>Test</a>"))
            {
                return Optional.Present(context);
            }
        }

        if (IsCommand("start"))
        {
            return Optional.Present(context);
        }

        return default;

        bool IsCommand(string commandName)
            =>
            string.Equals(text, commandName, StringComparison.InvariantCultureIgnoreCase);
    }
}