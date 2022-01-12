using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class TurnContextExtensions
{
    internal static async ValueTask<Unit> SendFailureAsync(this ITurnContext turnContext, FlowFailure failure, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(failure.UserMessage) is false)
        {
            var failureActivity = MessageFactory.Text(failure.UserMessage);
            _ = await turnContext.SendActivityAsync(failureActivity, cancellationToken).ConfigureAwait(false);
        }

        return default;
    }
}