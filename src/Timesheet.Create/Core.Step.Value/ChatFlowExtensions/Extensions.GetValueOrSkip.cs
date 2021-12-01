using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class ValueStepChatFlowExtensions
{
    public static ChatFlow<TNext> GetValueOrSkip<T, TValue, TNext>(
        this ChatFlow<T> chatFlow,
        Func<T, SkipActivityOption> optionFactory,
        Func<string, Result<TValue, Failure<Unit>>> parseValue,
        Func<T, TValue?, TNext> mapFlowState)
        where TValue : struct
        =>
        InnerGetValueOrSkip(
            chatFlow ?? throw new ArgumentNullException(nameof(chatFlow)),
            optionFactory ?? throw new ArgumentNullException(nameof(optionFactory)),
            parseValue ?? throw new ArgumentNullException(nameof(parseValue)),
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    private static ChatFlow<TNext> InnerGetValueOrSkip<T, TValue, TNext>(
        ChatFlow<T> chatFlow,
        Func<T, SkipActivityOption> optionFactory,
        Func<string, Result<TValue, Failure<Unit>>> parseValue,
        Func<T, TValue?, TNext> mapFlowState)
        where TValue : struct
        =>
        chatFlow.ForwardValue(
            optionFactory,
            (context, token) => context.GetValueOrRetryAsync(parseValue, token),
            mapFlowState);

    private static async ValueTask<ChatFlowAction<TValue?>> GetValueOrRetryAsync<TValue>(
        this IChatFlowContext<SkipActivityOption> context,
        Func<string, Result<TValue, Failure<Unit>>> parseValue,
        CancellationToken cancellationToken)
        where TValue : struct
    {
        var textResult = await context.GetTextOrRetryActionAsync<TValue?>(cancellationToken).ConfigureAwait(false);
        if (textResult.IsFailure)
        {
            return textResult.FailureOrThrow();
        }

        var text = textResult.SuccessOrThrow();
        if (text is null)
        {
            return ChatFlowAction.Next(default(TValue?));
        }

        var valueResult = parseValue.Invoke(text);
        if (valueResult.IsFailure)
        {
            var failure = valueResult.FailureOrThrow();
            _ = await context.SendFailureActivityAsync(failure, cancellationToken).ConfigureAwait(false);

            return context.AwaitAndRetrySameAction<TValue?>(default);
        }

        return ChatFlowAction.Next<TValue?>(valueResult.SuccessOrThrow());
    }
}