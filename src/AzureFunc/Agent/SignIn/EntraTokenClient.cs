using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

// The application authenticates with a client assertion: a Managed Identity token that is registered as a federated credential
internal sealed class EntraTokenClient(
    HttpClient httpClient,
    AgentSignInOption option,
    Func<CancellationToken, ValueTask<string>> clientAssertionProvider)
    : IEntraTokenClient
{
    internal const string ClientAssertionScope = "api://AzureADTokenExchange/.default";

    private const string ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";

    public ValueTask<EntraTokenSet> RedeemCodeAsync(string code, string codeVerifier, string scope, CancellationToken cancellationToken)
        =>
        SendAsync(
            new()
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["code_verifier"] = codeVerifier,
                ["redirect_uri"] = option.RedirectUri.AbsoluteUri,
                ["scope"] = scope
            },
            cancellationToken);

    public ValueTask<EntraTokenSet> RefreshAsync(string refreshToken, string scope, CancellationToken cancellationToken)
        =>
        SendAsync(
            new()
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = scope
            },
            cancellationToken);

    private async ValueTask<EntraTokenSet> SendAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        parameters["client_id"] = option.ClientId;
        parameters["client_assertion_type"] = ClientAssertionType;
        parameters["client_assertion"] = await clientAssertionProvider.Invoke(cancellationToken).ConfigureAwait(false);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"https://login.microsoftonline.com/{Uri.EscapeDataString(option.TenantId)}/oauth2/v2.0/token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TokenJson? token;
        try
        {
            token = JsonSerializer.Deserialize<TokenJson>(content, JsonSerializerOptions.Web);
        }
        catch (JsonException exception)
        {
            throw new EntraTokenException(
                $"The token endpoint has returned an unexpected response with the status {(int)response.StatusCode}.",
                isGrantInvalid: false,
                exception);
        }

        if (response.IsSuccessStatusCode is false || string.IsNullOrEmpty(token?.AccessToken))
        {
            // The description is logged by the caller; it never contains tokens
            throw new EntraTokenException(
                $"The token request has failed: {token?.Error}. {token?.ErrorDescription}",
                isGrantInvalid: string.Equals(token?.Error, "invalid_grant", StringComparison.Ordinal) ||
                    string.Equals(token?.Error, "interaction_required", StringComparison.Ordinal));
        }

        return new(token.AccessToken, token.RefreshToken, TimeSpan.FromSeconds(token.ExpiresIn));
    }

    private sealed record class TokenJson
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public string? Error { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }
}
