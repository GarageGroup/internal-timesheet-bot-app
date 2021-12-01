using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Infra.Bot.Builder;

public static partial class ValueStepChatFlowExtensions
{
    private static async ValueTask<Unit> SendFailureActivityAsync(
        this ITurnContext context, Failure<Unit> failure, CancellationToken token)
    {
        var failureMessage = failure.FailureMessage;
        if (string.IsNullOrEmpty(failureMessage))
        {
            return default;
        }

        var failureActivity = MessageFactory.Text(failureMessage);
        return Unit.From(await context.SendActivityAsync(failureActivity, token).ConfigureAwait(false));
    }

    private static async ValueTask<Result<string?, ChatFlowAction<T>>> GetTextOrRetryActionAsync<T>(
        this IChatFlowContext<SkipActivityOption> context,
        CancellationToken cancellationToken)
    {
        if (context.StepState is null)
        {
            var skupButtonId = Guid.NewGuid();

            var activity = context.CreateSkipActivity(context.FlowState, skupButtonId);
            await context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            return ChatFlowAction.AwaitAndRetry<T>(skupButtonId);
        }

        return context.GetTextOrFailure().MapFailure(context.AwaitAndRetrySameAction<T>);
    }
}