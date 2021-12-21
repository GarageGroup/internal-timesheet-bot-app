using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class UserAuthorizeFlow
{
    internal static async ValueTask<Result<UserDataJson, FlowFailure>> GetTeamsAuthorizationResultAsync(
        this ITurnContext context,
        IDataverseUserGetFunc dataverseUserGetFunc,
        ILogger logger,
        CancellationToken token)
    {
        try
        {
            var memberId = context.Activity.From.Id;
            var member = await TeamsInfo.GetMemberAsync(context, memberId, token).ConfigureAwait(false);

            if (member is null)
            {
                logger.LogError("Teams member cannot be found by Id: {memberId}", memberId);
                return new FlowFailure(UnexpectedFailureMessage);
            }

            var userId = Guid.Parse(member.AadObjectId);
            return await context.InnerGetDataverseAuthorizationResultAsync(dataverseUserGetFunc, logger, userId, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authorization in Teams has finished with an unexpected exception");
            return new FlowFailure(UnexpectedFailureMessage);
        }
    }
}