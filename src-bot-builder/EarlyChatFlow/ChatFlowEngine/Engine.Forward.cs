using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowEngine<T>
{
    internal ChatFlowEngine<TNext> InternalForwardValue<TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TNext>>> nextAsync)
        =>
        new(
            stepPosition: stepPosition + 1,
            chatFlowCache: chatFlowCache,
            turnContext: turnContext,
            flowStep: token => token.IsCancellationRequested ? InnerCanceledAsync<TNext>(token) : InnerGetNextAsync(nextAsync, token));

    private async ValueTask<ChatFlowResult<TNext>> InnerGetNextAsync<TNext>(
        Func<IChatFlowContext<T>, CancellationToken, ValueTask<ChatFlowAction<TNext>>> nextAsync,
        CancellationToken token)
    {
        var nextPosition = stepPosition + 1;
        var postionFromCache = await chatFlowCache.GetPositionAsync(token).ConfigureAwait(false);

        if (nextPosition < postionFromCache)
        {
            return ChatFlowResult<TNext>.Run(default!);
        }

        if (nextPosition == postionFromCache)
        {
            var cache = await chatFlowCache.GetStepCacheAsync<T>(token).ConfigureAwait(false);
            var context = new ChatFlowContextImpl<T>(turnContext, cache.FlowState!, cache.StepState);
            return await InnerGetNextResultAsync(context).ConfigureAwait(false);
        }

        var result = await flowStep.Invoke(token).ConfigureAwait(false);
        if (result.Code is ChatFlowResultCode.Running)
        {
            var context = new ChatFlowContextImpl<T>(turnContext, result.FlowStateOrThrow(), default);
            return await InnerGetNextResultAsync(context).ConfigureAwait(false);
        }

        return result.Code switch
        {
            ChatFlowResultCode.Awaiting => ChatFlowResult<TNext>.Await(),
            ChatFlowResultCode.Canceling => ChatFlowResult<TNext>.Cancel(),
            _ => default
        };

        async ValueTask<ChatFlowResult<TNext>> InnerGetNextResultAsync(IChatFlowContext<T> context)
        {
            var nextAction = await nextAsync.Invoke(context, token).ConfigureAwait(false);

            if (nextAction.Code is ChatFlowActionCode.AwaitingAndRetry)
            {
                var cache = new ChatFlowStepCacheJson<T>
                {
                    FlowState = context.FlowState,
                    StepState = nextAction.StepStateOrThrow()
                };
                _ = await chatFlowCache.SetStepCacheAsync(nextPosition, cache, token).ConfigureAwait(false);
                return ChatFlowResult<TNext>.Await();
            }
            else
            {
                _ = await chatFlowCache.ClearStepCacheAsync<T>(nextPosition, token).ConfigureAwait(false);
            }

            if (nextAction.Code is ChatFlowActionCode.Interruption)
            {
                _ = await chatFlowCache.ClearPositionAsync(token).ConfigureAwait(false);
                return ChatFlowResult<TNext>.Interrupt();
            }

            return nextAction.Code switch
            {
                ChatFlowActionCode.Next => ChatFlowResult<TNext>.Run(nextAction.FlowStateOrThrow()),
                ChatFlowActionCode.AwaitingAndRetry => ChatFlowResult<TNext>.Await(),
                ChatFlowActionCode.Canceling => ChatFlowResult<TNext>.Cancel(),
                _ => ChatFlowResult<TNext>.Interrupt()
            };
        }
    }

    private static ValueTask<ChatFlowResult<TNext>> InnerCanceledAsync<TNext>(CancellationToken token)
        =>
        ValueTask.FromCanceled<ChatFlowResult<TNext>>(token);
}