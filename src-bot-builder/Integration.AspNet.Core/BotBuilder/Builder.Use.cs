using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

partial class BotBuilder
{
    public IBotBuilder Use(Func<IBotContext, CancellationToken, ValueTask<TurnState>> middleware)
        =>
        InnerUse(
            middleware ?? throw new ArgumentNullException(nameof(middleware)));

    private BotBuilder InnerUse(Func<IBotContext, CancellationToken, ValueTask<TurnState>> middleware)
    {
        return new(
            serviceProvider,
            conversationState,
            userState,
            loggerFactory,
            new List<Func<ITurnContext, CancellationToken, ValueTask<TurnState>>>(middlewares)
            {
                InnerInvokeAsync
            });

        ValueTask<TurnState> InnerInvokeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var botContext = new BotContextImpl(turnContext, userState, conversationState, loggerFactory, serviceProvider);
            return middleware.Invoke(botContext, cancellationToken);
        }
    }
}