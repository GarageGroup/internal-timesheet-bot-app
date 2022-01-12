using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class OAuthFlowContextExtensions
{
    private static async ValueTask<Result<BotUser, FlowFailure>> InnerAuthorizeInDataverseAsync(
        this IOAuthFlowContext context,
        IDataverseUserGetFunc dataverseUserGetFunc,
        AzureUserGetOut azureUser,
        CancellationToken token)
    {
        var dataverseResult = await dataverseUserGetFunc.InvokeAsync(new(azureUser.Id), token).ConfigureAwait(false);
        return dataverseResult.Map(ToUserDataJson, ToFlowFailure);

        BotUser ToUserDataJson(DataverseUserGetOut dataverseUser)
            =>
            dataverseUser.ToUserDataJson(azureUser);

        FlowFailure ToFlowFailure(Failure<DataverseUserGetFailureCode> failure)
        {
            var failureMessage = failure.FailureMessage;
            context.GetLogger().LogError("Dataverse authoriation has failed with message: {failureMessage}", failureMessage);

            var displayMessage = failure.FailureCode is DataverseUserGetFailureCode.NotFound
                ? UserNotFoundFailureMessage : FlowFailure.UnexpectedFailureMessage;
            return new(displayMessage);
        }
    }
}