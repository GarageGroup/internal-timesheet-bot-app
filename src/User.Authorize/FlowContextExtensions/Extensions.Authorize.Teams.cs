using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class OAuthFlowContextExtensions
{
    internal static async ValueTask<Result<BotUser, FlowFailure>> AuthorizeInTeamsAsync(
        this IOAuthFlowContext context,
        IDataverseUserGetFunc dataverseUserGetFunc,
        CancellationToken token)
    {
        var logger = context.GetLogger();

        try
        {
            var memberId = context.Activity.From.Id;
            var member = await TeamsInfo.GetMemberAsync(context, memberId, token).ConfigureAwait(false);

            if (member is null)
            {
                logger.LogError("Teams member cannot be found by Id: {memberId}", memberId);
                return new FlowFailure(FlowFailure.UnexpectedFailureMessage);
            }

            var azureUser = new AzureUserGetOut(
                id: Guid.Parse(member.AadObjectId),
                mail: member.Email,
                displayName: member.Name);

            return await context.InnerAuthorizeInDataverseAsync(dataverseUserGetFunc, azureUser, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Authorization in Teams has finished with an unexpected exception");
            return new FlowFailure(FlowFailure.UnexpectedFailureMessage);
        }
    }
}