using System;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

public sealed partial class ChatFlow
{
    public static ChatFlow Create(ITurnContext turnContext, ConversationState conversationState, string chatFlowId)
        =>
        new(
            turnContext ?? throw new ArgumentNullException(nameof(turnContext)),
            conversationState ?? throw new ArgumentNullException(nameof(conversationState)),
            chatFlowId ?? string.Empty);

    private readonly ITurnContext turnContext;

    private readonly IChatFlowCache chatFlowCache;

    private readonly string chatFlowId;

    private ChatFlow(ITurnContext turnContext, ConversationState conversationState, string chatFlowId)
    {
        this.turnContext = turnContext;
        chatFlowCache = new ChatFlowCacheImpl(chatFlowId, conversationState, turnContext);
        this.chatFlowId = chatFlowId;
    }
}