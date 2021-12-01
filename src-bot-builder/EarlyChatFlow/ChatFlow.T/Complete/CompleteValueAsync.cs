using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow<T>
{
    public ValueTask<TurnState> CompleteValueAsync(CancellationToken cancellationToken)
        =>
        cancellationToken.IsCancellationRequested
        ? ValueTask.FromCanceled<TurnState>(cancellationToken)
        : chatFlowEngine.InternalCompleteValueAsync(cancellationToken);
}