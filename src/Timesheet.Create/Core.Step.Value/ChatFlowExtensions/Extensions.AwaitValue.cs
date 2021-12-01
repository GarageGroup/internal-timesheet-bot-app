using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ValueStepChatFlowExtensions
{
    public static ChatFlow<TNext> AwaitValue<T, TValue, TNext>(
        this ChatFlow<T> chatFlow,
        Func<string, Result<TValue, Failure<Unit>>> parseValueOrAbsent,
        Func<T, TValue, TNext> mapFlowState)
        =>
        InnerAwaitValue(
            chatFlow ?? throw new ArgumentNullException(nameof(chatFlow)),
            parseValueOrAbsent ?? throw new ArgumentNullException(nameof(parseValueOrAbsent)),
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    private static ChatFlow<TNext> InnerAwaitValue<T, TValue, TNext>(
        ChatFlow<T> chatFlow,
        Func<string, Result<TValue, Failure<Unit>>> parseValueOrAbsent,
        Func<T, TValue, TNext> mapFlowState)
        =>
        chatFlow.Await().ForwardValue(
            Unit.From,
            (context, token) => context.GetRequiredValueOrRetryAsync(parseValueOrAbsent, token),
            mapFlowState);

    private static async ValueTask<ChatFlowAction<TValue>> GetRequiredValueOrRetryAsync<T, TValue>(
        this IChatFlowContext<T> context,
        Func<string, Result<TValue, Failure<Unit>>> parseValueOrAbsent,
        CancellationToken cancellationToken)
    {
        var valueResult = await context.Activity
            .GetRequiredTextOrFailure()
            .MapFailure(MapAbsentFailure)
            .Forward(parseValueOrAbsent)
            .MapFailureValueAsync(SendFailureActivityAsync)
            .ConfigureAwait(false);

        return valueResult.Fold(ChatFlowAction.Next, context.AwaitAndRetrySameAction<TValue>);

        ValueTask<Unit> SendFailureActivityAsync(Failure<Unit> failure)
            =>
            context.SendFailureActivityAsync(failure, cancellationToken);

        static Failure<Unit> MapAbsentFailure(Unit _) => default;
    }
}