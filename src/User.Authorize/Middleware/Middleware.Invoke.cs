using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

partial class UserAuthorizeMiddleware
{
    public async ValueTask<Unit> InvokeAsync(ITurnContext turnContext, CancellationToken token = default)
    {
        if (await IsAlreadyAuthorizedAsync(token).ConfigureAwait(false))
        {
            return await botFlow.NextAsync(token).ConfigureAwait(false);
        }

        var flowContext = CreateFlowContext(turnContext);

        if (turnContext.IsTeamsChannel())
        {
            return await AuthorizeInTeamsAsync(flowContext, token).ConfigureAwait(false);
        }

        return await AuthorizeInNotTeamsAsync(flowContext, token).ConfigureAwait(false);
    }

    private async ValueTask<bool> IsAlreadyAuthorizedAsync(CancellationToken cancellationToken)
    {
        var currentUser = await botUserProvider.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        if (currentUser is not null)
        {
            if (currentUser.GetDataverseUserIdOrAbsent().IsPresent)
            {
                return true;
            }

            _ = await botUserProvider.SetCurrentUserAsync(default, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async ValueTask<Unit> AuthorizeInTeamsAsync(IOAuthFlowContext flowContext, CancellationToken cancellationToken)
    {
        var teamsResult = await flowContext.AuthorizeInTeamsAsync(dataverseUserGetFunc, cancellationToken).ConfigureAwait(false);
        return await teamsResult.FoldValueAsync(NextForTeamsAsync, SendFailureAsync).ConfigureAwait(false);

        async ValueTask<Unit> NextForTeamsAsync(BotUser botUser)
        {
            _ = await botUserProvider.SetCurrentUserAsync(botUser, cancellationToken).ConfigureAwait(false);
            return await botFlow.NextAsync(cancellationToken).ConfigureAwait(false);
        }

        ValueTask<Unit> SendFailureAsync(FlowFailure failure)
            =>
            flowContext.SendFailureAsync(failure, cancellationToken);
    }

    private async ValueTask<Unit> AuthorizeInNotTeamsAsync(IOAuthFlowContext flowContext, CancellationToken cancellationToken)
    {
        var sourceActivity = await sourceActivityAccessor.GetAsync(flowContext, default, cancellationToken).ConfigureAwait(false);
        if (sourceActivity is not null)
        {
            var tokenResult = await flowContext.RecognizeTokenOrFailureAsync(connectionName, cancellationToken).ConfigureAwait(false);
            var sendFailureResult = await tokenResult.MapFailureValueAsync(SendFailureAsync).ConfigureAwait(false);

            return await sendFailureResult.FoldValueAsync(AzureAuthAsync, SendOAuthCardOrBreakAsync).ConfigureAwait(false);
        }

        await sourceActivityAccessor.SetAsync(flowContext, flowContext.Activity, cancellationToken).ConfigureAwait(false);
        return await SendOAuthCardOrBreakAsync(default).ConfigureAwait(false);

        async ValueTask<Unit> AzureAuthAsync(TokenResponse tokenResponse)
        {
            var azureResult = await flowContext.AuthorizeInAzureAsync(
                azureUserGetFunc, dataverseUserGetFunc, tokenResponse, cancellationToken).ConfigureAwait(false);

            return await azureResult.FoldValueAsync(NextAsync, BreakAsync).ConfigureAwait(false);
        }

        async ValueTask<Unit> NextAsync(BotUser botUser)
        {
            var activity = MessageFactory.Text($"Привет, {botUser.GetUserName()}! Авторизация прошла успешно!");
            _ = await flowContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            _ = await botUserProvider.SetCurrentUserAsync(botUser, cancellationToken).ConfigureAwait(false);

            await sourceActivityAccessor.DeleteAsync(flowContext, cancellationToken).ConfigureAwait(false);
            return await botFlow.NextAsync(sourceActivity, cancellationToken).ConfigureAwait(false);
        }

        async ValueTask<Unit> SendOAuthCardOrBreakAsync(Unit _)
        {
            var sendResult = await flowContext.SendOAuthCardOrFailureAsync(connectionName, cancellationToken).ConfigureAwait(false);
            return await sendResult.FoldValueAsync(ValueTask.FromResult, BreakAsync).ConfigureAwait(false);
        }

        async ValueTask<Unit> BreakAsync(FlowFailure flowFailure)
        {
            await sourceActivityAccessor.DeleteAsync(flowContext, cancellationToken).ConfigureAwait(false);
            return await SendFailureAsync(flowFailure).ConfigureAwait(false);
        }

        ValueTask<Unit> SendFailureAsync(FlowFailure failure)
            =>
            flowContext.SendFailureAsync(failure, cancellationToken);
    }
}