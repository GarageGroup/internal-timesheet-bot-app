using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace GGroupp.Internal.Timesheet;

partial class OAuthActivityExtensions
{
    internal static async Task<ResourceResponse> SendOAuthActivityAsync(
        this ITurnContext turnContext, OAuthCardOptionJson option, CancellationToken cancellationToken)
    {
        if (turnContext.TurnState.ContainsKey(TurnStateConstants.OAuthLoginTimeoutKey) is false && option.Timeout.HasValue)
        {
            turnContext.TurnState.Add<object>(TurnStateConstants.OAuthLoginTimeoutKey, option.Timeout.Value);
        }

        var userTokenClient = turnContext.GetUserTokenClientOrThrow();
        var signInResource = await userTokenClient.GetSignInResourceAsync(option.ConnectionName, turnContext.Activity, default, cancellationToken).ConfigureAwait(false);

        var activity = CreateOAuthCard(turnContext, signInResource, option).ToActivity(inputHint: InputHints.AcceptingInput);
        return await turnContext.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
    }

    private static UserTokenClient GetUserTokenClientOrThrow(this ITurnContext turnContext)
        =>
        turnContext.TurnState.Get<UserTokenClient>() ?? throw new NotSupportedException("OAuth prompt is not supported by the current adapter");

    private static Attachment CreateOAuthCard(ITurnContext turnContext, SignInResource signInResource, OAuthCardOptionJson option)
        =>
        turnContext.Activity.IsOAuthCardSupported()
        ? CreateOAuthCardAttachment(signInResource, option, turnContext.Activity.IsEmulator())
        : CreateSigninCardAttachment(signInResource, option);

    private static Attachment CreateOAuthCardAttachment(SignInResource signInResource, OAuthCardOptionJson option, bool isEmulator)
        =>
        new()
        {
            ContentType = OAuthCard.ContentType,
            Content = new OAuthCard
            {
                Text = option.Text,
                ConnectionName = option.ConnectionName,
                Buttons = new[]
                {
                    new CardAction
                    {
                        Title = option.Title,
                        Text = option.Text,
                        Type = isEmulator ? ActionTypes.OpenUrl : ActionTypes.Signin,
                        Value = signInResource.SignInLink
                    }
                },
                TokenExchangeResource = signInResource.TokenExchangeResource,
            }
        };

    private static Attachment CreateSigninCardAttachment(SignInResource signInResource, OAuthCardOptionJson option)
        =>
        new()
        {
            ContentType = SigninCard.ContentType,
            Content = new SigninCard
            {
                Text = option.Text,
                Buttons = new[]
                {
                    new CardAction
                    {
                        Title = option.Title,
                        Value = signInResource.SignInLink,
                        Type = ActionTypes.Signin
                    }
                }
            }
        };
}