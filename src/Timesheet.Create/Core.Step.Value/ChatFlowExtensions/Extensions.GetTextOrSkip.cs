using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ValueStepChatFlowExtensions
{
    public static ChatFlow<TNext> GetTextOrSkip<T, TNext>(
        this ChatFlow<T> chatFlow,
        Func<T, SkipActivityOption> optionFactory,
        Func<T, string?, TNext> mapFlowState)
        =>
        InnerGetTextOrSkip(
            chatFlow ?? throw new ArgumentNullException(nameof(chatFlow)),
            optionFactory ?? throw new ArgumentNullException(nameof(optionFactory)),
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    private static ChatFlow<TNext> InnerGetTextOrSkip<T, TNext>(
        ChatFlow<T> chatFlow,
        Func<T, SkipActivityOption> optionFactory,
        Func<T, string?, TNext> mapFlowState)
        =>
        chatFlow.ForwardValue(
            optionFactory, GetTextOrRetryAsync, mapFlowState);

    private static async ValueTask<ChatFlowAction<string?>> GetTextOrRetryAsync(
        IChatFlowContext<SkipActivityOption> context, CancellationToken cancellationToken)
        =>
        Pipeline.Pipe(
            await context.GetTextOrRetryActionAsync<string?>(cancellationToken).ConfigureAwait(false))
        .Fold(
            ChatFlowAction.Next,
            Pipeline.Pipe);
}