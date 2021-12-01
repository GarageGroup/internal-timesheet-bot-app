using System;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial struct ChatFlowAction<T>
{
    public ValueTask<ChatFlowAction<TResult>> MapFlowStateValueAsync<TResult>(Func<T, ValueTask<TResult>> mapFlowStateAsync)
        =>
        InternalMapFlowStateValueAsync(
            mapFlowStateAsync ?? throw new ArgumentNullException(nameof(mapFlowStateAsync)));

    internal async ValueTask<ChatFlowAction<TResult>> InternalMapFlowStateValueAsync<TResult>(Func<T, ValueTask<TResult>> mapFlowStateAsync)
        =>
        Code is ChatFlowActionCode.Next
            ? new(await mapFlowStateAsync.Invoke(flowState).ConfigureAwait(false))
            : new(Code);
}