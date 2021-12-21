using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

partial class UserAuthorizeMiddleware
{
    public async ValueTask<TurnState> InvokeAsync(ITurnContext context, CancellationToken token = default)
    {
        var userData = await userStateProvider.GetUserDataAsync(context, token).ConfigureAwait(false);
        if (userData is not null)
        {
            return TurnState.Completed;
        }

        if (context.Activity.IsTeams())
        {
            var teamsResult = await context.GetTeamsAuthorizationResultAsync(dataverseUserGetFunc, logger, token).ConfigureAwait(false);
            return await teamsResult.FoldValueAsync(ToSuccessStateAsync, ToFailureStateAsync).ConfigureAwait(false);
        }

        var flowState = await userStateProvider.GetFlowStateAsync(context, token).ConfigureAwait(false);
        if (flowState.Option is not null)
        {
            var result = await context.RecognizeTokenAsync(flowState.Option.Value, flowState.CallerInfo, token).ConfigureAwait(false);
            if (result.IsFailure)
            {
                var retrySettings = flowState.Option.Value with
                {
                    Text = "Не удалось авторизавоться. Повторите попытку"
                };
                _ = await context.SendOAuthActivityAsync(retrySettings, token).ConfigureAwait(false);

                return TurnState.Awaiting;
            }

            var azureResult = await azureUserGetFunc.InvokeAsync(new(result.SuccessOrThrow().Token), token).ConfigureAwait(false);
            return await azureResult.FoldValueAsync(ToDataverseAuthStateAsync, GetFailureStateAsync).ConfigureAwait(false);
        }

        var option = GetOAuthSettings("Войдите в свою учетную запись");
        _ = await context.SendOAuthActivityAsync(option, token).ConfigureAwait(false);

        var callerInfo = CreateCallerInfo(context);
        var state = CreateFlowState(option, callerInfo);

        _ = await userStateProvider.SaveFlowStateAsync(context, state, token).ConfigureAwait(false);
        return TurnState.Awaiting;

        async ValueTask<TurnState> ToSuccessStateAsync(UserDataJson userData)
        {
            await userStateProvider.SaveUserDataAsync(context, userData, token).ConfigureAwait(false);
            return TurnState.Completed;
        }

        async ValueTask<TurnState> ToFailureStateAsync(FlowFailure failure)
        {
            if (string.IsNullOrEmpty(failure.UserMessage) is false)
            {
                var failureActivity = MessageFactory.Text(failure.UserMessage);
                _ = await context.SendActivityAsync(failureActivity, token).ConfigureAwait(false);
            }

            _ = await userStateProvider.ClearAsync(context, token).ConfigureAwait(false);
            return TurnState.Interrupted;
        }
    }

    private static CallerInfoJson? CreateCallerInfo(ITurnContext turnContext)
    {
        if (turnContext.TurnState.Get<ClaimsIdentity>(BotAdapter.BotIdentityKey) is ClaimsIdentity botIdentity
            && SkillValidation.IsSkillClaim(botIdentity.Claims))
        {
            return new()
            {
                CallerServiceUrl = turnContext.Activity.ServiceUrl,
                Scope = JwtTokenValidation.GetAppIdFromClaims(botIdentity.Claims),
            };
        }

        return default;
    }
}