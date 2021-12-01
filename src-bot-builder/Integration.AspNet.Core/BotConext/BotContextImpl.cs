using System;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Infra.Bot.Builder;

internal sealed class BotContextImpl : IBotContext
{
    internal BotContextImpl(
        ITurnContext turnContext,
        UserState userState,
        ConversationState conversationState,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        TurnContext = turnContext;
        UserState = userState;
        ConversationState = conversationState;
        LoggerFactory = loggerFactory;
        ServiceProvider = serviceProvider;
    }

    public ITurnContext TurnContext { get; }

    public UserState UserState { get; }

    public ConversationState ConversationState { get; }

    public ILoggerFactory LoggerFactory { get; }

    public IServiceProvider ServiceProvider { get; }
}