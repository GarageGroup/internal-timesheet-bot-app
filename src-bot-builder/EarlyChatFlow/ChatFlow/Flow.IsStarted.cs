using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ChatFlow
{
    public ValueTask<bool> IsStartedAsync(CancellationToken cancellationToken)
        =>
        cancellationToken.IsCancellationRequested
        ? ValueTask.FromCanceled<bool>(cancellationToken)
        : InnerIsStartedAsync(cancellationToken);

    private async ValueTask<bool> InnerIsStartedAsync(CancellationToken cancellationToken)
    {
        var position = await chatFlowCache.GetPositionAsync(cancellationToken).ConfigureAwait(false);
        return position >= 0;
    }
}