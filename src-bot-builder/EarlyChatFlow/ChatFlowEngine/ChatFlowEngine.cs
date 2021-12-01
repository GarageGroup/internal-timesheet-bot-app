using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

internal sealed partial class ChatFlowEngine<T>
{
    private readonly int stepPosition;

    private readonly IChatFlowCache chatFlowCache;

    private readonly ITurnContext turnContext;

    private readonly Func<CancellationToken, ValueTask<ChatFlowResult<T>>> flowStep;

    internal ChatFlowEngine(
        int stepPosition,
        IChatFlowCache chatFlowCache,
        ITurnContext turnContext,
        Func<CancellationToken, ValueTask<ChatFlowResult<T>>> flowStep)
    {
        this.stepPosition = stepPosition;
        this.chatFlowCache = chatFlowCache;
        this.turnContext = turnContext;
        this.flowStep = flowStep;
    }
}