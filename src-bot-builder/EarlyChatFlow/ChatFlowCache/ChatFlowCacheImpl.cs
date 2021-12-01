using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using static System.FormattableString;

namespace GGroupp.Infra.Bot.Builder;

internal sealed class ChatFlowCacheImpl : IChatFlowCache
{
    private const int DefaultPosition = -1;

    private readonly string chatFlowId;

    private readonly ConversationState conversationState;

    private readonly ITurnContext turnContext;

    private readonly IStatePropertyAccessor<int> positionAccessor;

    private int? positionCache;

    internal ChatFlowCacheImpl(string chatFlowId, ConversationState conversationState, ITurnContext turnContext)
    {
        this.chatFlowId = chatFlowId ?? string.Empty;
        this.conversationState = conversationState;
        this.turnContext = turnContext;
        positionAccessor = conversationState.CreateProperty<int>($"__{chatFlowId}Position");
    }

    public async ValueTask<int> GetPositionAsync(CancellationToken cancellationToken)
    {
        if (positionCache is null)
        {
            positionCache = await positionAccessor.GetAsync(turnContext, () => DefaultPosition, cancellationToken).ConfigureAwait(false);
        }

        return positionCache.Value;
    }

    public async Task<Unit> ClearPositionAsync(CancellationToken cancellationToken)
    {
        positionCache = null;
        await positionAccessor.DeleteAsync(turnContext, cancellationToken).ConfigureAwait(false);
        return default;
    }

    public async Task<ChatFlowStepCacheJson<T>> GetStepCacheAsync<T>(CancellationToken cancellationToken)
    {
        var position = await GetPositionAsync(cancellationToken).ConfigureAwait(false);
        var accessor = CreateStepCacheAccessor<T>(position);

        return await accessor.GetAsync(turnContext, () => default, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Unit> ClearStepCacheAsync<T>(int position, CancellationToken cancellationToken)
    {
        var currentPosition = await GetPositionAsync(cancellationToken).ConfigureAwait(false);
        if (currentPosition != position)
        {
            return default;
        }

        var accessor = CreateStepCacheAccessor<T>(position);

        await accessor.DeleteAsync(turnContext, cancellationToken).ConfigureAwait(false);
        return default;
    }

    public async Task<Unit> SetStepCacheAsync<T>(int position, ChatFlowStepCacheJson<T> cacheJson, CancellationToken cancellationToken)
    {
        positionCache = position;
        await positionAccessor.SetAsync(turnContext, position, cancellationToken).ConfigureAwait(false);

        var accessor = CreateStepCacheAccessor<T>(position);
        await accessor.SetAsync(turnContext, cacheJson, cancellationToken).ConfigureAwait(false);

        return default;
    }

    private IStatePropertyAccessor<ChatFlowStepCacheJson<T>> CreateStepCacheAccessor<T>(int position)
        =>
        conversationState.CreateProperty<ChatFlowStepCacheJson<T>>(Invariant($"__{chatFlowId}State{position}"));
}