using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Configuration;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentAccessTokenHandler(TokenCredential credential, IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var audience = configuration["AgentApi:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("AgentApi:Audience must be configured.");
        }

        var scope = audience.TrimEnd('/') + "/.default";
        var accessToken = await credential.GetTokenAsync(new([scope]), cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
