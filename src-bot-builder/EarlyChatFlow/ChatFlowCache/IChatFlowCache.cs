using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

internal interface IChatFlowCache
{
    ValueTask<int> GetPositionAsync(CancellationToken cancellationToken);

    Task<Unit> ClearPositionAsync(CancellationToken cancellationToken);

    Task<ChatFlowStepCacheJson<T>> GetStepCacheAsync<T>(CancellationToken cancellationToken);

    ValueTask<Unit> ClearStepCacheAsync<T>(int position, CancellationToken cancellationToken);

    Task<Unit> SetStepCacheAsync<T>(int position, ChatFlowStepCacheJson<T> cacheJson, CancellationToken cancellationToken);
}