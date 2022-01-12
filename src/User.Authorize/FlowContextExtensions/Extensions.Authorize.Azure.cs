using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;
using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class OAuthFlowContextExtensions
{
    internal static async ValueTask<Result<BotUser, FlowFailure>> AuthorizeInAzureAsync(
        this IOAuthFlowContext context,
        IAzureUserGetFunc azureUserGetFunc,
        IDataverseUserGetFunc dataverseUserGetFunc,
        TokenResponse tokenResponse,
        CancellationToken cancellationToken)
    {
        var azureIn = new AzureUserMeGetIn(tokenResponse.Token);
        var azureResult = await azureUserGetFunc.InvokeAsync(azureIn, cancellationToken).ConfigureAwait(false);

        return await azureResult.MapFailure(ToFlowFailure).ForwardValueAsync(ToUserDataJsonAsync).ConfigureAwait(false);

        ValueTask<Result<BotUser, FlowFailure>> ToUserDataJsonAsync(AzureUserGetOut user)
            =>
            context.InnerAuthorizeInDataverseAsync(dataverseUserGetFunc, user, cancellationToken);

        FlowFailure ToFlowFailure(Failure<AzureUserGetFailureCode> failure)
        {
            var failureMessage = failure.FailureMessage;
            context.GetLogger().LogError("Azure authoriation has failed with message: {failureMessage}", failureMessage);

            return new(FlowFailure.UnexpectedFailureMessage);
        }
    }
}