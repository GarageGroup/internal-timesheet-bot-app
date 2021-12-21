using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class UserAuthorizeFlow
{
    private static async ValueTask<Result<UserDataJson, FlowFailure>> InnerGetDataverseAuthorizationResultAsync(
        this ITurnContext context,
        IDataverseUserGetFunc dataverseUserGetFunc,
        ILogger logger,
        Guid azureUserId,
        CancellationToken token)
    {
        var dataverseResult = await dataverseUserGetFunc.InvokeAsync(new(azureUserId), token).ConfigureAwait(false);
        return dataverseResult.Map(ToUserDataJson, ToFlowFailure);

        UserDataJson ToUserDataJson(DataverseUserGetOut user)
            =>
            new()
            {
                ActiveDirectoryId = azureUserId,
                DataverseUserId = user.SystemUserId,
                DataverseUserFirstName = user.FirstName,
                DataverseUserLastName = user.LastName,
                DataverseUserFullName = user.FullName
            };

        FlowFailure ToFlowFailure(Failure<DataverseUserGetFailureCode> failure)
        {
            var failureMessage = failure.FailureMessage;
            logger.LogError("Dataverse authoriation has failed with message: {failureMessage}", failureMessage);

            var displayMessage = failure.FailureCode is DataverseUserGetFailureCode.NotFound ? UserNotFoundFailureMessage : UnexpectedFailureMessage;
            return new(displayMessage);
        }
    }
}