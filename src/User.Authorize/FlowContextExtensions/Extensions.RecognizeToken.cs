using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

partial class OAuthFlowContextExtensions
{
    internal static ValueTask<Result<TokenResponse, FlowFailure>> RecognizeTokenOrFailureAsync(
        this IOAuthFlowContext context, string connectionName, CancellationToken cancellationToken)
    {
        var activity = context.Activity;
        if (activity.IsNotMessageType())
        {
            return GetUnsuccessfulTokenFailureResultAsync();
        }

        if (string.IsNullOrEmpty(activity.Text))
        {
            return GetUnsuccessfulTokenFailureResultAsync();
        }

        var matchedMagicCode = Regex.Match(input: activity.Text, pattern: @"(\d{6})");
        if (matchedMagicCode.Success is false)
        {
            return GetUnsuccessfulTokenFailureResultAsync();
        }

        return context.GetUserTokenPrividerOrFailure().ForwardValueAsync(InnerGetUserTokenOrFailureAsync);

        async ValueTask<Result<TokenResponse, FlowFailure>> InnerGetUserTokenOrFailureAsync(
            IExtendedUserTokenProvider userTokenProvider)
        {
            try
            {
                var userToken = await userTokenProvider.GetUserTokenAsync(
                    turnContext: context,
                    connectionName: connectionName,
                    magicCode: matchedMagicCode.Value,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (userToken is null)
                {
                    return new FlowFailure(UnsuccessfulTokenFailureMessage);
                }

                return userToken;
            }
            catch (Exception ex)
            {
                context.GetLogger().LogError(ex, "An unexpected exception was thrown by userTokenProvider.GetUserTokenAsync");
                return new FlowFailure(FlowFailure.UnexpectedFailureMessage);
            }
        }

        static ValueTask<Result<TokenResponse, FlowFailure>> GetUnsuccessfulTokenFailureResultAsync()
            =>
            ValueTask.FromResult<Result<TokenResponse, FlowFailure>>(
                new FlowFailure(UnsuccessfulTokenFailureMessage));
    }
}