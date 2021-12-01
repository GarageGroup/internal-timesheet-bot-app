using System;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Infra.Bot.Builder;

public interface IBotContext
{
    ITurnContext TurnContext { get; }

    UserState UserState { get; }

    ConversationState ConversationState { get; }

    ILoggerFactory LoggerFactory { get; }

    IServiceProvider ServiceProvider { get; }
}