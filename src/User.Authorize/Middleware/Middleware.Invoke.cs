using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
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

        if (context.Activity.ChannelId is Channels.Msteams)
        {
            var member = await TeamsInfo.GetMemberAsync(context, context.Activity.From.Id, token).ConfigureAwait(false);
            if (member is not null)
            {
                var azureUserId = Guid.Parse(member.AadObjectId);
                return await AuthorizeInDataverseAsync(context, azureUserId, token).ConfigureAwait(false);
            }
        }

        var flowState = await userStateProvider.GetFlowStateAsync(context, token).ConfigureAwait(false);
        if (flowState.Settings is not null)
        {
            var result = await OAuthPrompt.RecognizeTokenAsync(flowState.Settings, context, token).ConfigureAwait(false);
            /*if (result.Succeeded is false)
            {
                var retrySettings = GetOAuthSettings("Не удалось авторизавоться. Повторите попытку");
                await OAuthPrompt.SendOAuthCardAsync(retrySettings, context.Context, null, token).ConfigureAwait(false);

                return ChatFlowStepResult.RetryAndAwait<string>();
            }

            return result.Value.Token ?? string.Empty;*/
        }

        
        if (flowState.Settings is null)
        {
            var settings = GetOAuthSettings("Войдите в свою учетную запись");
            var state = CreateFlowState(settings);

            await OAuthPrompt.SendOAuthCardAsync(settings, context, default, token).ConfigureAwait(false);
            await userStateProvider.SaveFlowStateAsync(context, state, token).ConfigureAwait(false);

            return TurnState.Awaiting;
        }
    }

    private async ValueTask<TurnState> AuthorizeInDataverseAsync(ITurnContext context, Guid azureUserId, CancellationToken token)
    {
        var dataverseResult = await dataverseUserGetFunc.InvokeAsync(new(azureUserId), token).ConfigureAwait(false);
        return await dataverseResult.FoldValueAsync(GetSuccessStateAsync, GetFailureStateAsync).ConfigureAwait(false);

        async ValueTask<TurnState> GetSuccessStateAsync(DataverseUserGetOut user)
        {
            var userData = new UserDataJson
            {
                ActiveDirectoryId = azureUserId,
                DataverseUserId = user.SystemUserId,
                DataverseUserFirstName = user.FirstName,
                DataverseUserLastName = user.LastName,
                DataverseUserFullName = user.FullName
            };

            await userStateProvider.SaveUserDataAsync(context, userData, token).ConfigureAwait(false);
            return TurnState.Completed;
        }

        async ValueTask<TurnState> GetFailureStateAsync(Failure<DataverseUserGetFailureCode> failure)
        {
            var failureMessage = failure.FailureMessage;
            logger.LogError("An unexpected failure: {failureMessage}", failureMessage);
            var failureActivity = MessageFactory.Text(CreateFailureMessage(failure.FailureCode));

            _ = await context.SendActivityAsync(failureActivity, token).ConfigureAwait(false);
            _ = await userStateProvider.ClearAsync(context, token).ConfigureAwait(false);

            return TurnState.Interrupted;
        }

        static string CreateFailureMessage(DataverseUserGetFailureCode failureCode)
            =>
            failureCode is DataverseUserGetFailureCode.NotFound
            ? "Пользователь не найден. Возомжно у вас нет прав для доступа в систему"
            : "Возникла непредвиденная ошибка при попытке получить данные пользователя. Возможно сервер временно не доступен. Повторите попытку позже";
    }
}