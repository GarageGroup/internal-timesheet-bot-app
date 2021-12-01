using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

partial class LookupStepChatFlowExtensions
{
    public static ChatFlow<TNext> AwaitLookupValue<T, TNext>(
        this ChatFlow<T> chatFlow,
        LookupValueSetDefaultFunc<T> defaultItemsFunc,
        LookupValueSetSearchFunc<T> searchFunc,
        Func<T, LookupValue, TNext> mapFlowState)
        =>
        InnerAwaitLookupValue(
            chatFlow ?? throw new ArgumentNullException(nameof(chatFlow)),
            defaultItemsFunc ?? throw new ArgumentNullException(nameof(defaultItemsFunc)),
            searchFunc ?? throw new ArgumentNullException(nameof(searchFunc)),
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    private static ChatFlow<TNext> InnerAwaitLookupValue<T, TNext>(
        ChatFlow<T> chatFlow,
        LookupValueSetDefaultFunc<T> defaultItemsFunc,
        LookupValueSetSearchFunc<T> searchFunc,
        Func<T, LookupValue, TNext> mapFlowState)
        =>
        chatFlow.ForwardValue(
            (context, token) => context.GetChoosenValueOrRetryAsync(defaultItemsFunc, searchFunc, token),
            mapFlowState);

    private static async ValueTask<ChatFlowAction<LookupValue>> GetChoosenValueOrRetryAsync<T>(
        this IChatFlowContext<T> context,
        LookupValueSetDefaultFunc<T>? defaultItemsFunc,
        LookupValueSetSearchFunc<T> searchFunc,
        CancellationToken token)
    {
        var flowState = context.FlowState;

        if (context.StepState is null)
        {
            if (defaultItemsFunc is null)
            {
                return ChatFlowAction.AwaitAndRetry<LookupValue>(new());
            }

            var defaultValueSet = await defaultItemsFunc.Invoke(flowState, token).ConfigureAwait(false);
            var defaulActivity = context.CreateLookupActivity(defaultValueSet);

            _ = await context.SendActivityAsync(defaulActivity, token).ConfigureAwait(false);
            return defaultValueSet.ToAwaitAndRetryWithLookupCacheAction();
        }

        var choosenItemResult = context.GetChoosenValueOrAbsent();
        if (choosenItemResult.IsPresent)
        {
            return choosenItemResult.Map(ChatFlowAction.Next).OrThrow();
        }

        var searchText = context.Activity.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            return context.AwaitAndRetrySameAction<LookupValue>(default);
        }

        var searchResult = await searchFunc.Invoke(flowState, new(searchText), token).ConfigureAwait(false);
        if (searchResult.IsFailure)
        {
            var searchFailureMessage = searchResult.FailureOrThrow().FailureMessage;
            if (string.IsNullOrEmpty(searchFailureMessage) is false)
            {
                var failureActivity = MessageFactory.Text(searchFailureMessage);
                _ = await context.SendActivityAsync(failureActivity, token).ConfigureAwait(false);
            }

            return context.AwaitAndRetrySameAction<LookupValue>(default);
        }

        var lookupValueSet = searchResult.SuccessOrThrow();

        var successActivity = context.CreateLookupActivity(lookupValueSet);
        _ = await context.SendActivityAsync(successActivity, token).ConfigureAwait(false);

        return lookupValueSet.ToAwaitAndRetryWithLookupCacheAction();
    }
}