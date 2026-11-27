using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentSignInService(
    AgentSignInOption option,
    string signInAudience,
    IAgentSignInStore store,
    IEntraTokenClient tokenClient,
    IAgentTokenProtector protector,
    TimeProvider timeProvider,
    ILogger logger)
    : IAgentSignInService, IAgentUserTokenProvider
{
    private static readonly TimeSpan AccessTokenReserve = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<(long UserId, string Scope), CachedAccessToken> accessTokens = new();

    public async ValueTask<Uri> CreateSignInUrlAsync(AgentSignInRequest request, CancellationToken cancellationToken)
    {
        var state = CreateRandomValue();
        var codeVerifier = CreateRandomValue();

        await store.SavePendingAsync(
            new(
                State: state,
                User: request.User,
                UserDisplayName: request.UserDisplayName,
                CodeVerifier: codeVerifier,
                Locale: request.Locale,
                SourceUpdateJson: request.SourceUpdateJson,
                ExpiresAt: timeProvider.GetUtcNow().Add(option.PendingTtl)),
            cancellationToken).ConfigureAwait(false);

        var codeChallenge = ToBase64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        // The code is returned in the request body: API Management overrides the 'code' query parameter with the function key
        var builder = new StringBuilder("https://login.microsoftonline.com/")
            .Append(Uri.EscapeDataString(option.TenantId))
            .Append("/oauth2/v2.0/authorize?response_type=code&response_mode=form_post&prompt=select_account")
            .Append("&client_id=").Append(Uri.EscapeDataString(option.ClientId))
            .Append("&redirect_uri=").Append(Uri.EscapeDataString(option.RedirectUri.AbsoluteUri))
            .Append("&scope=").Append(Uri.EscapeDataString(GetSignInScope()))
            .Append("&state=").Append(state)
            .Append("&code_challenge_method=S256&code_challenge=").Append(codeChallenge);

        return new(builder.ToString());
    }

    public async ValueTask<AgentPendingSignIn?> FindPendingAsync(string state, CancellationToken cancellationToken)
    {
        var pending = await store.GetPendingAsync(state, cancellationToken).ConfigureAwait(false);
        if (pending is null)
        {
            return null;
        }

        if (pending.ExpiresAt > timeProvider.GetUtcNow())
        {
            return pending;
        }

        await store.DeletePendingAsync(state, cancellationToken).ConfigureAwait(false);
        return null;
    }

    public async ValueTask<AgentPendingSignIn?> CompleteAsync(string state, string code, CancellationToken cancellationToken)
    {
        var pending = await FindPendingAsync(state, cancellationToken).ConfigureAwait(false);
        if (pending is null)
        {
            return null;
        }

        // The link is one-time: it is removed even if the code exchange fails
        await store.DeletePendingAsync(state, cancellationToken).ConfigureAwait(false);

        EntraTokenSet tokenSet;
        try
        {
            tokenSet = await tokenClient.RedeemCodeAsync(code, pending.CodeVerifier, GetSignInScope(), cancellationToken).ConfigureAwait(false);
        }
        catch (EntraTokenException exception)
        {
            logger.LogWarning(exception, "Failed to redeem the authorization code of the Telegram user {userId}", pending.User.TelegramUserId);
            return null;
        }

        if (string.IsNullOrEmpty(tokenSet.RefreshToken))
        {
            logger.LogError("The token endpoint has not returned a refresh token. The offline_access scope must be granted");
            return null;
        }

        await SaveTokensAsync(pending.User.TelegramUserId, GetScope(signInAudience), tokenSet, cancellationToken).ConfigureAwait(false);
        return pending;
    }

    public async ValueTask SignOutAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        RemoveCachedTokens(telegramUserId);
        await store.DeleteRefreshTokenAsync(telegramUserId, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<string?> GetAccessTokenAsync(AgentUserContext user, string audience, CancellationToken cancellationToken)
    {
        var scope = GetScope(audience);
        var userId = user.TelegramUserId;

        if (accessTokens.TryGetValue((userId, scope), out var cached) && cached.ExpiresAt > timeProvider.GetUtcNow())
        {
            return cached.Value;
        }

        var protectedRefreshToken = await store.GetRefreshTokenAsync(userId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(protectedRefreshToken))
        {
            return null;
        }

        var refreshToken = protector.Unprotect(protectedRefreshToken);
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("The stored refresh token of the Telegram user {userId} could not be decrypted", userId);
            await SignOutAsync(userId, cancellationToken).ConfigureAwait(false);

            return null;
        }

        EntraTokenSet tokenSet;
        try
        {
            tokenSet = await tokenClient.RefreshAsync(refreshToken, scope + " offline_access", cancellationToken).ConfigureAwait(false);
        }
        catch (EntraTokenException exception) when (exception.IsGrantInvalid)
        {
            logger.LogInformation(exception, "The refresh token of the Telegram user {userId} is no longer valid", userId);
            await SignOutAsync(userId, cancellationToken).ConfigureAwait(false);

            return null;
        }

        await SaveTokensAsync(userId, scope, tokenSet, cancellationToken).ConfigureAwait(false);
        return tokenSet.AccessToken;
    }

    private async ValueTask SaveTokensAsync(long userId, string scope, EntraTokenSet tokenSet, CancellationToken cancellationToken)
    {
        // Entra rotates refresh tokens, so the latest one replaces the stored value
        if (string.IsNullOrEmpty(tokenSet.RefreshToken) is false)
        {
            await store.SaveRefreshTokenAsync(userId, protector.Protect(tokenSet.RefreshToken), cancellationToken).ConfigureAwait(false);
        }

        var lifetime = tokenSet.ExpiresIn - AccessTokenReserve;
        if (lifetime > TimeSpan.Zero)
        {
            accessTokens[(userId, scope)] = new(tokenSet.AccessToken, timeProvider.GetUtcNow().Add(lifetime));
        }
    }

    private void RemoveCachedTokens(long userId)
    {
        foreach (var key in accessTokens.Keys)
        {
            if (key.UserId == userId)
            {
                _ = accessTokens.TryRemove(key, out _);
            }
        }
    }

    private string GetSignInScope()
        =>
        "openid offline_access " + GetScope(signInAudience);

    private static string GetScope(string audience)
        =>
        audience.TrimEnd('/') + "/.default";

    private static string CreateRandomValue()
        =>
        ToBase64Url(RandomNumberGenerator.GetBytes(32));

    private static string ToBase64Url(byte[] value)
        =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record class CachedAccessToken(string Value, DateTimeOffset ExpiresAt);
}
