using System;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public Task<ChatFlowAction<TResult>> MapFlowStateAsync<TResult>(Func<T, Task<TResult>> mapFlowStateAsync)
        =>
        InternalMapFlowStateAsync(
            mapFlowStateAsync ?? throw new ArgumentNullException(nameof(mapFlowStateAsync)));

    internal async Task<ChatFlowAction<TResult>> InternalMapFlowStateAsync<TResult>(Func<T, Task<TResult>> mapFlowStateAsync)
        =>
        Code is ChatFlowActionCode.Next
            ? new(await mapFlowStateAsync.Invoke(flowState).ConfigureAwait(false))
            : new(Code);
}