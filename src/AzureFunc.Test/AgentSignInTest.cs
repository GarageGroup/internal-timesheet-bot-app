using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentSignInTest
{
    private const string Audience = "api://timesheet";

    private static readonly AgentUserContext User = new(TelegramUserId: 15, TelegramChatId: 27);

    private static readonly AgentSignInOption Option = new(
        TenantId: "tenant-id",
        ClientId: "client-id",
        RedirectUri: new("https://apim.example/oauth2/callback"),
        TokenEncryptionKey: Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray());

    [Fact]
    public static void Protector_ProtectedValue_ExpectRoundTripAndRejectionOfChangedOrForeignValues()
    {
        var protector = new AesGcmAgentTokenProtector(Option.TokenEncryptionKey);
        var protectedValue = protector.Protect("refresh-token");

        Assert.DoesNotContain("refresh-token", protectedValue);
        Assert.NotEqual(protectedValue, protector.Protect("refresh-token"));
        Assert.Equal("refresh-token", protector.Unprotect(protectedValue));

        var changed = Convert.FromBase64String(protectedValue);
        changed[^1] ^= 1;

        Assert.Null(protector.Unprotect(Convert.ToBase64String(changed)));
        Assert.Null(protector.Unprotect("not base64"));
        Assert.Null(new AesGcmAgentTokenProtector(new byte[32]).Unprotect(protectedValue));
        _ = Assert.Throws<ArgumentException>(() => new AesGcmAgentTokenProtector(new byte[16]));
    }

    [Fact]
    public static async Task TokenClient_RedeemCode_ExpectFormWithClientAssertionAndParsedTokens()
    {
        var handler = new FakeHttpMessageHandler(
            static _ => FakeHttpMessageHandler.Json("""{"access_token":"access","refresh_token":"refresh","expires_in":3600}"""));

        var client = new EntraTokenClient(new(handler), Option, static _ => new("assertion"));
        var actual = await client.RedeemCodeAsync("auth-code", "verifier", "openid scope", TestContext.Current.CancellationToken);

        Assert.Equal(new("access", "refresh", TimeSpan.FromHours(1)), actual);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://login.microsoftonline.com/tenant-id/oauth2/v2.0/token", request.Uri.AbsoluteUri);

        var form = HttpUtility.ParseQueryString(request.Body);
        Assert.Equal("authorization_code", form["grant_type"]);
        Assert.Equal("auth-code", form["code"]);
        Assert.Equal("verifier", form["code_verifier"]);
        Assert.Equal("https://apim.example/oauth2/callback", form["redirect_uri"]);
        Assert.Equal("openid scope", form["scope"]);
        Assert.Equal("client-id", form["client_id"]);
        Assert.Equal("assertion", form["client_assertion"]);
        Assert.Equal("urn:ietf:params:oauth:client-assertion-type:jwt-bearer", form["client_assertion_type"]);
        Assert.Null(form["client_secret"]);
    }

    [Theory]
    [InlineData("invalid_grant", true)]
    [InlineData("interaction_required", true)]
    [InlineData("invalid_client", false)]
    public static async Task TokenClient_EndpointReturnsError_ExpectExceptionWithGrantFlag(string error, bool expectedIsGrantInvalid)
    {
        var handler = new FakeHttpMessageHandler(
            _ => FakeHttpMessageHandler.Json($$"""{"error":"{{error}}","error_description":"failure"}""", HttpStatusCode.BadRequest));

        var client = new EntraTokenClient(new(handler), Option, static _ => new("assertion"));

        var exception = await Assert.ThrowsAsync<EntraTokenException>(
            async () => await client.RefreshAsync("refresh", "scope", TestContext.Current.CancellationToken));

        Assert.Equal(expectedIsGrantInvalid, exception.IsGrantInvalid);
        Assert.Equal("refresh_token", HttpUtility.ParseQueryString(handler.Requests[0].Body)["grant_type"]);
    }

    [Fact]
    public static async Task CreateSignInUrlAsync_ExpectAuthorizeUrlWithPkceAndSavedPendingSignIn()
    {
        var context = new SignInContext();

        var url = await context.Service.CreateSignInUrlAsync(
            new(User, "Ivan (@ivan)", "ru", """{"updateId":1}"""), TestContext.Current.CancellationToken);

        Assert.StartsWith("https://login.microsoftonline.com/tenant-id/oauth2/v2.0/authorize?", url.AbsoluteUri);

        var query = HttpUtility.ParseQueryString(url.Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("form_post", query["response_mode"]);
        Assert.Equal("client-id", query["client_id"]);
        Assert.Equal("https://apim.example/oauth2/callback", query["redirect_uri"]);
        Assert.Equal("openid offline_access api://timesheet/.default", query["scope"]);
        Assert.Equal("S256", query["code_challenge_method"]);

        var pending = Assert.Single(context.Store.Pending.Values);
        Assert.Equal(query["state"], pending.State);
        Assert.Equal(43, pending.State.Length);
        Assert.Equal(User, pending.User);
        Assert.Equal("Ivan (@ivan)", pending.UserDisplayName);
        Assert.Equal("""{"updateId":1}""", pending.SourceUpdateJson);
        Assert.Equal(context.Time.Now.AddMinutes(10), pending.ExpiresAt);

        var expectedChallenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(pending.CodeVerifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal(expectedChallenge, query["code_challenge"]);
    }

    [Fact]
    public static async Task CompleteAsync_ValidState_ExpectProtectedRefreshTokenIsStoredAndLinkIsOneTime()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();
        var codeVerifier = context.Store.Pending[state].CodeVerifier;

        var actual = await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken);

        Assert.Equal(User, actual?.User);
        Assert.Equal([("redeem", "auth-code", codeVerifier, "openid offline_access api://timesheet/.default")], context.TokenClient.Calls);
        Assert.Empty(context.Store.Pending);

        var storedToken = context.Store.RefreshTokens[User.TelegramUserId];
        Assert.NotEqual("refresh-1", storedToken);
        Assert.Equal("refresh-1", context.Protector.Unprotect(storedToken));

        Assert.Null(await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken));
        _ = Assert.Single(context.TokenClient.Calls);

        // The access token received during the sign-in is reused
        Assert.Equal("access-1", await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));
        _ = Assert.Single(context.TokenClient.Calls);
    }

    [Fact]
    public static async Task CompleteAsync_LinkIsExpired_ExpectNullAndNoTokenRequest()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();

        context.Time.Now = context.Time.Now.AddMinutes(10);

        Assert.Null(await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken));
        Assert.Empty(context.TokenClient.Calls);
        Assert.Empty(context.Store.Pending);
        Assert.Empty(context.Store.RefreshTokens);
    }

    [Fact]
    public static async Task CompleteAsync_CodeIsRejected_ExpectNullAndLinkIsRemoved()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();

        context.TokenClient.Failure = new EntraTokenException("invalid code", isGrantInvalid: true);

        Assert.Null(await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken));
        Assert.Empty(context.Store.Pending);
        Assert.Empty(context.Store.RefreshTokens);
    }

    [Fact]
    public static async Task GetAccessTokenAsync_UserHasNotSignedIn_ExpectNull()
    {
        var context = new SignInContext();

        Assert.Null(await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));
        Assert.Empty(context.TokenClient.Calls);
    }

    [Fact]
    public static async Task GetAccessTokenAsync_StoredRefreshToken_ExpectRefreshRotationAndCacheUntilExpiration()
    {
        var context = new SignInContext();
        context.Store.RefreshTokens[User.TelegramUserId] = context.Protector.Protect("refresh-0");

        var first = await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken);
        var second = await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken);

        Assert.Equal("access-1", first);
        Assert.Equal("access-1", second);
        Assert.Equal([("refresh", "refresh-0", string.Empty, "api://timesheet/.default offline_access")], context.TokenClient.Calls);
        Assert.Equal("refresh-1", context.Protector.Unprotect(context.Store.RefreshTokens[User.TelegramUserId]));

        // The cached token is not used during the last five minutes of its lifetime
        context.Time.Now = context.Time.Now.AddMinutes(55);
        var third = await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken);

        Assert.Equal("access-2", third);
        Assert.Equal(("refresh", "refresh-1", string.Empty, "api://timesheet/.default offline_access"), context.TokenClient.Calls[^1]);

        // Another audience gets its own access token from the same refresh token
        var other = await context.Service.GetAccessTokenAsync(User, "api://other/", TestContext.Current.CancellationToken);

        Assert.Equal("access-3", other);
        Assert.Equal(("refresh", "refresh-2", string.Empty, "api://other/.default offline_access"), context.TokenClient.Calls[^1]);
    }

    [Fact]
    public static async Task GetAccessTokenAsync_RefreshTokenIsRevoked_ExpectNullAndStoredTokenIsRemoved()
    {
        var context = new SignInContext();
        context.Store.RefreshTokens[User.TelegramUserId] = context.Protector.Protect("refresh-0");
        context.TokenClient.Failure = new EntraTokenException("revoked", isGrantInvalid: true);

        Assert.Null(await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));
        Assert.Empty(context.Store.RefreshTokens);
    }

    [Fact]
    public static async Task GetAccessTokenAsync_TokenEndpointIsUnavailable_ExpectExceptionAndStoredTokenIsKept()
    {
        var context = new SignInContext();
        context.Store.RefreshTokens[User.TelegramUserId] = context.Protector.Protect("refresh-0");
        context.TokenClient.Failure = new EntraTokenException("unavailable", isGrantInvalid: false);

        _ = await Assert.ThrowsAsync<EntraTokenException>(
            async () => await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));

        _ = Assert.Single(context.Store.RefreshTokens);
    }

    [Fact]
    public static async Task GetAccessTokenAsync_StoredTokenWasEncryptedWithAnotherKey_ExpectNullAndStoredTokenIsRemoved()
    {
        var context = new SignInContext();
        context.Store.RefreshTokens[User.TelegramUserId] = new AesGcmAgentTokenProtector(new byte[32]).Protect("refresh-0");

        Assert.Null(await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));
        Assert.Empty(context.Store.RefreshTokens);
        Assert.Empty(context.TokenClient.Calls);
    }

    [Fact]
    public static async Task SignOutAsync_SignedInUser_ExpectStoredAndCachedTokensAreRemoved()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();
        _ = await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken);

        await context.Service.SignOutAsync(User.TelegramUserId, TestContext.Current.CancellationToken);

        Assert.Empty(context.Store.RefreshTokens);
        Assert.Null(await context.Service.GetAccessTokenAsync(User, Audience, TestContext.Current.CancellationToken));
    }

    [Fact]
    public static async Task CallbackHandler_FirstRequest_ExpectConfirmationPageAndSignInIsNotCompleted()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync("Ivan <b>");
        var notifier = new FakeNotifier();
        var handler = new AgentSignInCallbackHandler(context.Service, notifier, NullLogger.Instance);

        var actual = await handler.HandleAsync(
            new Dictionary<string, string> { ["state"] = state, ["code"] = "auth\"code" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
        Assert.Contains("Подтвердите вход", actual.Html);
        Assert.Contains("Ivan &lt;b&gt;", actual.Html);
        Assert.DoesNotContain("Ivan <b>", actual.Html);
        Assert.Contains("value=\"auth&quot;code\"", actual.Html);
        Assert.Contains($"value=\"{state}\"", actual.Html);

        Assert.Empty(context.TokenClient.Calls);
        Assert.Empty(notifier.Notified);
        _ = Assert.Single(context.Store.Pending);
    }

    [Fact]
    public static async Task CallbackHandler_ConfirmedRequest_ExpectSignInIsCompletedAndUserIsNotified()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();
        var notifier = new FakeNotifier();
        var handler = new AgentSignInCallbackHandler(context.Service, notifier, NullLogger.Instance);

        var actual = await handler.HandleAsync(
            new Dictionary<string, string> { ["state"] = state, ["code"] = "auth-code", ["confirm"] = "1" },
            TestContext.Current.CancellationToken);

        Assert.Contains("Вход выполнен", actual.Html);
        Assert.Contains("<meta http-equiv=\"refresh\" content=\"1;url=tg://resolve?domain=timesheet_test_bot\"/>", actual.Html);
        Assert.Contains("href=\"https://t.me/timesheet_test_bot\"", actual.Html);
        Assert.Equal(User, Assert.Single(notifier.Notified).User);
        _ = Assert.Single(context.Store.RefreshTokens);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bot\"/><script>")]
    public static async Task CallbackHandler_BotUsernameIsUnknownOrInvalid_ExpectSuccessPageWithoutRedirect(string? botUsername)
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();
        var handler = new AgentSignInCallbackHandler(
            context.Service, new FakeNotifier { BotUsername = botUsername }, NullLogger.Instance);

        var actual = await handler.HandleAsync(
            new Dictionary<string, string> { ["state"] = state, ["code"] = "auth-code", ["confirm"] = "1" },
            TestContext.Current.CancellationToken);

        Assert.Contains("Вход выполнен", actual.Html);
        Assert.DoesNotContain("refresh", actual.Html);
        Assert.DoesNotContain("t.me", actual.Html);
        Assert.DoesNotContain("<script>", actual.Html);
    }

    [Fact]
    public static async Task SetSignInMessageAsync_PendingSignIn_ExpectMessageIdIsReturnedOnCompletion()
    {
        var context = new SignInContext();
        var url = await context.Service.CreateSignInUrlAsync(new(User, "Ivan", "ru", "{}"), TestContext.Current.CancellationToken);
        var state = HttpUtility.ParseQueryString(url.Query)["state"]!;

        await context.Service.SetSignInMessageAsync(url, 345, TestContext.Current.CancellationToken);
        await context.Service.SetSignInMessageAsync(new("https://login.example/?state=unknown"), 999, TestContext.Current.CancellationToken);

        var completed = await context.Service.CompleteAsync(state, "auth-code", TestContext.Current.CancellationToken);

        Assert.Equal(345, completed?.SignInMessageId);
        Assert.Empty(context.Store.Pending);
    }

    [Theory]
    [InlineData("unknown-state", "auth-code", null, HttpStatusCode.OK)]
    [InlineData("", "auth-code", null, HttpStatusCode.BadRequest)]
    [InlineData("state", "", null, HttpStatusCode.BadRequest)]
    [InlineData("state", "", "access_denied", HttpStatusCode.OK)]
    public static async Task CallbackHandler_InvalidRequest_ExpectFailurePageAndNoSignIn(
        string state, string code, string? error, HttpStatusCode expectedStatusCode)
    {
        var context = new SignInContext();
        _ = await context.StartSignInAsync();
        var notifier = new FakeNotifier();
        var handler = new AgentSignInCallbackHandler(context.Service, notifier, NullLogger.Instance);

        var form = new Dictionary<string, string> { ["state"] = state, ["code"] = code, ["confirm"] = "1" };
        if (error is not null)
        {
            form["error"] = error;
        }

        var actual = await handler.HandleAsync(form, TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatusCode, actual.StatusCode);
        Assert.Contains("Не удалось войти", actual.Html);
        Assert.Empty(context.Store.RefreshTokens);
        Assert.Empty(notifier.Notified);
    }

    [Fact]
    public static async Task CallbackHandler_NotifierFails_ExpectSuccessPage()
    {
        var context = new SignInContext();
        var state = await context.StartSignInAsync();
        var handler = new AgentSignInCallbackHandler(
            context.Service, new FakeNotifier { Failure = new InvalidOperationException("telegram failure") }, NullLogger.Instance);

        var actual = await handler.HandleAsync(
            new Dictionary<string, string> { ["state"] = state, ["code"] = "auth-code", ["confirm"] = "1" },
            TestContext.Current.CancellationToken);

        Assert.Contains("Вход выполнен", actual.Html);
        _ = Assert.Single(context.Store.RefreshTokens);
    }

    private sealed class SignInContext
    {
        internal SignInContext()
            =>
            Service = new(Option, Audience, Store, TokenClient, Protector, Time, NullLogger.Instance);

        internal FakeSignInStore Store { get; } = new();

        internal FakeTokenClient TokenClient { get; } = new();

        internal AesGcmAgentTokenProtector Protector { get; } = new(Option.TokenEncryptionKey);

        internal MutableTimeProvider Time { get; } = new();

        internal AgentSignInService Service { get; }

        internal async Task<string> StartSignInAsync(string displayName = "Ivan")
        {
            var url = await Service.CreateSignInUrlAsync(new(User, displayName, "ru", "{}"), TestContext.Current.CancellationToken);
            return HttpUtility.ParseQueryString(url.Query)["state"]!;
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
            =>
            Now;
    }

    private sealed class FakeNotifier : IAgentSignInNotifier
    {
        internal List<AgentPendingSignIn> Notified { get; } = [];

        internal Exception? Failure { get; init; }

        internal string? BotUsername { get; init; } = "timesheet_test_bot";

        public ValueTask<string?> GetBotUsernameAsync(CancellationToken cancellationToken)
            =>
            new(BotUsername);

        public ValueTask NotifySignedInAsync(AgentPendingSignIn pending, CancellationToken cancellationToken)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Notified.Add(pending);
            return default;
        }
    }

    private sealed class FakeTokenClient : IEntraTokenClient
    {
        internal List<(string Grant, string Value, string CodeVerifier, string Scope)> Calls { get; } = [];

        internal EntraTokenException? Failure { get; set; }

        public ValueTask<EntraTokenSet> RedeemCodeAsync(string code, string codeVerifier, string scope, CancellationToken cancellationToken)
            =>
            Respond(("redeem", code, codeVerifier, scope));

        public ValueTask<EntraTokenSet> RefreshAsync(string refreshToken, string scope, CancellationToken cancellationToken)
            =>
            Respond(("refresh", refreshToken, string.Empty, scope));

        private ValueTask<EntraTokenSet> Respond((string Grant, string Value, string CodeVerifier, string Scope) call)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Calls.Add(call);
            return new(new EntraTokenSet($"access-{Calls.Count}", $"refresh-{Calls.Count}", TimeSpan.FromHours(1)));
        }
    }

    private sealed class FakeSignInStore : IAgentSignInStore
    {
        internal Dictionary<string, AgentPendingSignIn> Pending { get; } = [];

        internal Dictionary<long, string> RefreshTokens { get; } = [];

        public ValueTask SavePendingAsync(AgentPendingSignIn pending, CancellationToken cancellationToken)
        {
            Pending[pending.State] = pending;
            return default;
        }

        public ValueTask<AgentPendingSignIn?> GetPendingAsync(string state, CancellationToken cancellationToken)
            =>
            new(Pending.GetValueOrDefault(state));

        public ValueTask DeletePendingAsync(string state, CancellationToken cancellationToken)
        {
            _ = Pending.Remove(state);
            return default;
        }

        public ValueTask<string?> GetRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken)
            =>
            new(RefreshTokens.GetValueOrDefault(telegramUserId));

        public ValueTask SaveRefreshTokenAsync(long telegramUserId, string protectedRefreshToken, CancellationToken cancellationToken)
        {
            RefreshTokens[telegramUserId] = protectedRefreshToken;
            return default;
        }

        public ValueTask DeleteRefreshTokenAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            _ = RefreshTokens.Remove(telegramUserId);
            return default;
        }
    }
}
