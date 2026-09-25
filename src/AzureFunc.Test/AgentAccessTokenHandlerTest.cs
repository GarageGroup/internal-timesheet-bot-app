using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentAccessTokenHandlerTest
{
    [Fact]
    public static async Task SendAsync_ExpectConfiguredScopeAndBearerToken()
    {
        var credential = new StubTokenCredential();
        var terminal = new StubHttpMessageHandler(static request =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("some-token", request.Headers.Authorization?.Parameter);
            return new(HttpStatusCode.OK);
        });

        using var handler = new AgentAccessTokenHandler(
            credential,
            BuildConfiguration("api://some-api/"))
        {
            InnerHandler = terminal
        };

        using var client = new HttpClient(handler);
        using var response = await client.GetAsync("https://example.com", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("api://some-api/.default", Assert.Single(credential.Scopes));
    }

    private static IConfiguration BuildConfiguration(string audience)
        =>
        new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["AgentApi:Audience"] = audience
            }).Build();

    private sealed class StubTokenCredential : TokenCredential
    {
        internal string[] Scopes { get; private set; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            =>
            throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            Scopes = requestContext.Scopes;
            return ValueTask.FromResult(new AccessToken("some-token", DateTimeOffset.MaxValue));
        }
    }
}
