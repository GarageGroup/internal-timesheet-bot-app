using System;
using System.Threading;
using System.Threading.Tasks;

namespace GGroupp.Infra.Bot.Builder;

partial class LookupStepChatFlowExtensions
{
    public static ChatFlow<TNext> AwaitChoiceValue<T, TNext>(
        this ChatFlow<T> chatFlow,
        Func<T, LookupValueSetSeachOut> choiceSetFactory,
        Func<T, LookupValue, TNext> mapFlowState)
        =>
        InnerAwaitChoiceValue(
            chatFlow ?? throw new ArgumentNullException(nameof(chatFlow)),
            choiceSetFactory ?? throw new ArgumentNullException(nameof(choiceSetFactory)),
            mapFlowState ?? throw new ArgumentNullException(nameof(mapFlowState)));

    private static ChatFlow<TNext> InnerAwaitChoiceValue<T, TNext>(
        ChatFlow<T> chatFlow,
        Func<T, LookupValueSetSeachOut> choiceSetFactory,
        Func<T, LookupValue, TNext> mapFlowState)
        =>
        chatFlow.ForwardValue(
            (context, token) => context.GetChoosenValueOrRetryAsync(choiceSetFactory, token),
            mapFlowState);

    private static async ValueTask<ChatFlowAction<LookupValue>> GetChoosenValueOrRetryAsync<T>(
        this IChatFlowContext<T> context,
        Func<T, LookupValueSetSeachOut> choiceSetFactory,
        CancellationToken token)
    {
        if (context.StepState is null)
        {
            var choiceSet = choiceSetFactory.Invoke(context.FlowState);
            var setActivity = context.CreateLookupActivity(choiceSet);

            _ = await context.SendActivityAsync(setActivity, token).ConfigureAwait(false);
            return choiceSet.ToAwaitAndRetryWithLookupCacheAction();
        }

        return context.GetChoosenValueOrAbsent().Fold(
            ChatFlowAction.Next,
            () => context.AwaitAndRetrySameAction<LookupValue>(default));
    }
}