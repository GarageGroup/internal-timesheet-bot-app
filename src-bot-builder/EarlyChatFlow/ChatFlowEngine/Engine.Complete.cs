using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlowEngine<T>
{
    internal async ValueTask<TurnState> InternalCompleteValueAsync(CancellationToken cancellationToken)
    {
        var result = await flowStep.Invoke(cancellationToken).ConfigureAwait(false);
        if (result.Code is not ChatFlowResultCode.Awaiting)
        {
            _ = await chatFlowCache.ClearPositionAsync(cancellationToken).ConfigureAwait(false);
        }

        return result.Code switch
        {
            ChatFlowResultCode.Running => TurnState.Completed,
            ChatFlowResultCode.Awaiting => TurnState.Awaiting,
            ChatFlowResultCode.Canceling => TurnState.Canceled,
            _ => TurnState.Interrupted
        };
    }
}