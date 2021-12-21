using System;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.OAuth;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace GGroupp.Internal.Timesheet;

partial class OAuthActivityExtensions
{
    internal static ValueTask<Result<TokenResponse, Unit>> RecognizeTokenAsync(
        this ITurnContext turnContext, OAuthCardOptionJson option, CallerInfoJson? callerInfo, CancellationToken cancellationToken)
    {
        if (turnContext.Activity.IsTokenResponseEvent())
        {
            return turnContext.RecognizeTokenResponseEventTokenAsync(callerInfo, cancellationToken);
        }

        if (turnContext.Activity.IsTeamsVerificationInvoke())
        {
            return turnContext.RecognizeTeamsVerificationInvokeTokenAsync(option, cancellationToken);
        }

        if (turnContext.Activity.IsTokenExchangeRequestInvoke())
        {
            return turnContext.RecognizeTokenExchangeRequestInvokeTokenAsync(option, cancellationToken);
        }

        if (turnContext.Activity.IsMessageType() && string.IsNullOrEmpty(turnContext.Activity.Text) is false)
        {
            return turnContext.RecognizeMagicCodeTokenAsync(option, cancellationToken);
        }

        return default;
    }

    private static async ValueTask<Result<TokenResponse, Unit>> RecognizeTokenResponseEventTokenAsync(
       this ITurnContext turnContext, CallerInfoJson? callerInfo, CancellationToken cancellationToken)
    {
        var tokenResponseObject = turnContext.Activity.Value as JObject;
        var token = tokenResponseObject?.ToObject<TokenResponse>() ?? new();

        // fixup the turnContext's state context if this was received from a skill host caller
        if (callerInfo is not null)
        {
            // set the ServiceUrl to the skill host's Url
            turnContext.Activity.ServiceUrl = callerInfo.CallerServiceUrl;

            // recreate a ConnectorClient and set it in TurnState so replies use the correct one
            var serviceUrl = turnContext.Activity.ServiceUrl ?? string.Empty;
            var claimsIdentity = turnContext.TurnState.Get<ClaimsIdentity>(BotAdapter.BotIdentityKey);
            var audience = callerInfo.Scope ?? string.Empty;
            var connectorClient = await turnContext.CreateConnectorClientAsync(serviceUrl, claimsIdentity, audience, cancellationToken).ConfigureAwait(false);
            if (turnContext.TurnState.Get<IConnectorClient>() is not null)
            {
                turnContext.TurnState.Set(connectorClient);
            }
            else
            {
                turnContext.TurnState.Add(connectorClient);
            }
        }

        return token;
    }

    private static async ValueTask<Result<TokenResponse, Unit>> RecognizeTeamsVerificationInvokeTokenAsync(
        this ITurnContext turnContext, OAuthCardOptionJson option, CancellationToken cancellationToken)
    {
        var magicCodeObject = turnContext.Activity.Value as JObject;
        var magicCode = magicCodeObject?.GetValue("state", StringComparison.Ordinal)?.ToString() ?? string.Empty;

        // Getting the token follows a different flow in Teams. At the signin completion, Teams
        // will send the bot an "invoke" activity that contains a "magic" code. This code MUST
        // then be used to try fetching the token from Botframework service within some time
        // period. We try here. If it succeeds, we return 200 with an empty body. If it fails
        // with a retriable error, we return 500. Teams will re-send another invoke in this case.
        // If it fails with a non-retriable error, we return 404. Teams will not (still work in
        // progress) retry in that case.
        try
        {
            var token = await turnContext.GetUserTokenAsync(option, magicCode, cancellationToken).ConfigureAwait(false);
            if (token is not null)
            {
                await turnContext.SendActivityAsync(new Activity { Type = ActivityTypesEx.InvokeResponse }, cancellationToken).ConfigureAwait(false);
                return token;
            }
            else
            {
                await turnContext.SendInvokeResponseAsync(HttpStatusCode.NotFound, default, cancellationToken).ConfigureAwait(false);
            }
        }
        #pragma warning disable CA1031 // Do not catch general exception types (ignoring exception for now and send internal server error, see comment above)
        catch
        #pragma warning restore CA1031 // Do not catch general exception types
        {
            await turnContext.SendInvokeResponseAsync(HttpStatusCode.InternalServerError, default, cancellationToken).ConfigureAwait(false);
        }

        return default;
    }

    private static async ValueTask<Result<TokenResponse, Unit>> RecognizeTokenExchangeRequestInvokeTokenAsync(
        this ITurnContext turnContext, OAuthCardOptionJson option, CancellationToken cancellationToken)
    {
        var tokenExchangeRequest = ((JObject)turnContext.Activity.Value)?.ToObject<TokenExchangeInvokeRequest>();

        if (tokenExchangeRequest is null)
        {
            await turnContext.SendInvokeResponseAsync(
                HttpStatusCode.BadRequest,
                new TokenExchangeInvokeResponse
                {
                    Id = default,
                    ConnectionName = option.ConnectionName,
                    FailureDetail = "The bot received an InvokeActivity that is missing a TokenExchangeInvokeRequest value. This is required to be sent with the InvokeActivity.",
                }, cancellationToken).ConfigureAwait(false);

            return default;
        }

        if (tokenExchangeRequest.ConnectionName.EqualsInvariant(option.ConnectionName) is false)
        {
            await turnContext.SendInvokeResponseAsync(
                HttpStatusCode.BadRequest,
                new TokenExchangeInvokeResponse
                {
                    Id = tokenExchangeRequest.Id,
                    ConnectionName = option.ConnectionName,
                    FailureDetail = "The bot received an InvokeActivity with a TokenExchangeInvokeRequest containing a ConnectionName that does not match the ConnectionName expected by the bot's active OAuthPrompt. Ensure these names match when sending the InvokeActivityInvalid ConnectionName in the TokenExchangeInvokeRequest",
                }, cancellationToken).ConfigureAwait(false);

            return default;
        }

        var exchangeRequest = new TokenExchangeRequest
        {
            Token = tokenExchangeRequest.Token
        };
        var tokenExchangeResponse = await turnContext.ExchangeTokenAsync(option, exchangeRequest, cancellationToken).ConfigureAwait(false);

        if (tokenExchangeResponse is null || string.IsNullOrEmpty(tokenExchangeResponse.Token))
        {
            await turnContext.SendInvokeResponseAsync(
                HttpStatusCode.PreconditionFailed,
                new TokenExchangeInvokeResponse
                {
                    Id = tokenExchangeRequest.Id,
                    ConnectionName = option.ConnectionName,
                    FailureDetail = "The bot is unable to exchange token. Proceed with regular login.",
                }, cancellationToken).ConfigureAwait(false);

            return default;
        }

        await turnContext.SendInvokeResponseAsync(
                HttpStatusCode.OK,
                new TokenExchangeInvokeResponse
                {
                    Id = tokenExchangeRequest.Id,
                    ConnectionName = option.ConnectionName,
                }, cancellationToken).ConfigureAwait(false);

        return new TokenResponse
        {
            ChannelId = tokenExchangeResponse.ChannelId,
            ConnectionName = tokenExchangeResponse.ConnectionName,
            Token = tokenExchangeResponse.Token,
        };
    }

    private static async ValueTask<Result<TokenResponse, Unit>> RecognizeMagicCodeTokenAsync(
        this ITurnContext turnContext, OAuthCardOptionJson option, CancellationToken cancellationToken)
    {
        // regex to check if code supplied is a 6 digit numerical code (hence, a magic code).
        var matched = magicCodeRegex.Match(turnContext.Activity.Text);
        if (matched.Success)
        {
            var token = await turnContext.GetUserTokenAsync(option, magicCode: matched.Value, cancellationToken).ConfigureAwait(false);
            if (token is not null)
            {
                return token;
            }
        }
        return default;
    }

    private static Task<IConnectorClient> CreateConnectorClientAsync(
        this ITurnContext turnContext, string serviceUrl, ClaimsIdentity claimsIdentity, string audience, CancellationToken cancellationToken)
    {
        var connectorFactory = turnContext.TurnState.Get<ConnectorFactory>();
        if (connectorFactory is not null)
        {
            return connectorFactory.CreateAsync(serviceUrl, audience, cancellationToken);
        }
        if (turnContext.Adapter is IConnectorClientBuilder connectorClientProvider)
        {
            return connectorClientProvider.CreateConnectorClientAsync(serviceUrl, claimsIdentity, audience, cancellationToken);
        }
        throw new NotSupportedException("OAuth prompt is not supported by the current adapter");
    }

    private static Task<TokenResponse> GetUserTokenAsync(this ITurnContext turnContext, OAuthCardOptionJson option, string magicCode, CancellationToken cancellationToken)
    {
        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        if (userTokenClient is null)
        {
            throw new NotSupportedException("OAuth prompt is not supported by the current adapter");
        }
        return userTokenClient.GetUserTokenAsync(turnContext.Activity.From.Id, option.ConnectionName, turnContext.Activity.ChannelId, magicCode, cancellationToken);
    }

    private static Task<TokenResponse> ExchangeTokenAsync(this ITurnContext turnContext, OAuthCardOptionJson option, TokenExchangeRequest tokenExchangeRequest, CancellationToken cancellationToken)
    {
        var userTokenClient = turnContext.TurnState.Get<UserTokenClient>();
        if (userTokenClient is null)
        {
            throw new NotSupportedException("OAuth prompt is not supported by the current adapter");
        }

        var userId = turnContext.Activity.From.Id;
        var channelId = turnContext.Activity.ChannelId;

        return userTokenClient.ExchangeTokenAsync(userId, option.ConnectionName, channelId, tokenExchangeRequest, cancellationToken);
    }


    private static Task SendInvokeResponseAsync(this ITurnContext turnContext, HttpStatusCode statusCode, object? body, CancellationToken cancellationToken)
        =>
        turnContext.SendActivityAsync(
            new Activity
            {
                Type = ActivityTypesEx.InvokeResponse,
                Value = new InvokeResponse
                {
                    Status = (int)statusCode,
                    Body = body,
                },
            },
            cancellationToken);
}