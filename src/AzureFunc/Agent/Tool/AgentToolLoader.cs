using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using ModelContextProtocol.Client;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentToolLoader : IAgentToolLoader
{
    private const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";

    private static readonly TimeSpan SpecificationTtl = TimeSpan.FromMinutes(10);

    private readonly AgentToolOption[] tools;

    private readonly HttpMessageHandler primaryHandler;

    private readonly HttpClient httpClient;

    private readonly IAgentUserTokenProvider tokenProvider;

    private readonly ILoggerFactory loggerFactory;

    private readonly TimeProvider timeProvider;

    private readonly ConcurrentDictionary<string, CachedSpecification> specifications = new(StringComparer.Ordinal);

    internal AgentToolLoader(
        AgentToolOption[] tools,
        HttpMessageHandler primaryHandler,
        IAgentUserTokenProvider tokenProvider,
        ILoggerFactory loggerFactory,
        TimeProvider? timeProvider = null)
    {
        this.tools = tools;
        this.primaryHandler = primaryHandler;
        this.tokenProvider = tokenProvider;
        this.loggerFactory = loggerFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;

        httpClient = new(primaryHandler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async ValueTask<AgentToolSet> LoadAsync(AgentUserContext user, CancellationToken cancellationToken)
    {
        var plugins = new List<KernelPlugin>(tools.Length);
        var resources = new List<IAsyncDisposable>();

        try
        {
            foreach (var tool in tools)
            {
                // The token is resolved before the model is called so that a missing sign-in is reported to the user
                // instead of being returned to the model as a tool failure
                var accessToken = await GetAccessTokenAsync(tool, user, cancellationToken).ConfigureAwait(false);

                var plugin = tool.Type switch
                {
                    AgentToolType.OpenApi => await LoadOpenApiPluginAsync(tool, accessToken, cancellationToken).ConfigureAwait(false),
                    _ => await LoadMcpPluginAsync(tool, accessToken, resources, cancellationToken).ConfigureAwait(false)
                };

                plugins.Add(plugin);
            }

            return new(plugins, resources);
        }
        catch
        {
            await new AgentToolSet([], resources).DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async ValueTask<string?> GetAccessTokenAsync(AgentToolOption tool, AgentUserContext user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(tool.Audience))
        {
            return null;
        }

        var accessToken = await tokenProvider.GetAccessTokenAsync(user, tool.Audience, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(accessToken) ? throw new AgentUserNotSignedInException(tool.Audience) : accessToken;
    }

    private async Task<KernelPlugin> LoadOpenApiPluginAsync(AgentToolOption tool, string? accessToken, CancellationToken cancellationToken)
    {
        var specification = await GetSpecificationAsync(tool, cancellationToken).ConfigureAwait(false);

        var parameters = new OpenApiFunctionExecutionParameters
        {
            HttpClient = httpClient,
            ServerUrlOverride = tool.BaseAddress,
            AuthCallback = (request, _) =>
            {
                Authorize(request.Headers, tool, accessToken);
                return Task.CompletedTask;
            },
            LoggerFactory = loggerFactory
        };

        if (tool.BaseAddress is not null)
        {
            // Calls are limited to the configured base address: it is the only host that may receive the user access token
#pragma warning disable SKEXP0040
            parameters.ServerUrlValidationOptions = new()
            {
                AllowedBaseUrls = [tool.BaseAddress]
            };
#pragma warning restore SKEXP0040
        }

        if (tool.AllowedOperations.Length > 0)
        {
            var allowedOperations = tool.AllowedOperations.ToHashSet(StringComparer.OrdinalIgnoreCase);
#pragma warning disable SKEXP0040
            parameters.OperationSelectionPredicate = context => allowedOperations.Contains(context.Id.OrEmpty());
#pragma warning restore SKEXP0040
        }

        using var stream = new MemoryStream(specification, writable: false);
        return await OpenApiKernelPluginFactory.CreateFromOpenApiAsync(tool.Name, stream, parameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<KernelPlugin> LoadMcpPluginAsync(
        AgentToolOption tool, string? accessToken, List<IAsyncDisposable> resources, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(tool.SubscriptionKey) is false)
        {
            headers[SubscriptionKeyHeaderName] = tool.SubscriptionKey;
        }

        if (string.IsNullOrEmpty(accessToken) is false)
        {
            headers["Authorization"] = "Bearer " + accessToken;
        }

        var transport = new HttpClientTransport(
            new()
            {
                Endpoint = tool.Url,
                Name = tool.Name,
                AdditionalHeaders = headers
            },
            new HttpClient(primaryHandler, disposeHandler: false),
            loggerFactory,
            ownsHttpClient: true);

        var client = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: cancellationToken).ConfigureAwait(false);
        resources.Add(client);

        var mcpTools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var allowedOperations = tool.AllowedOperations.ToHashSet(StringComparer.OrdinalIgnoreCase);

#pragma warning disable SKEXP0001
        var functions = mcpTools
            .Where(mcpTool => allowedOperations.Count is 0 || allowedOperations.Contains(mcpTool.Name))
            .Select(static mcpTool => mcpTool.AsKernelFunction());
#pragma warning restore SKEXP0001

        return KernelPluginFactory.CreateFromFunctions(tool.Name, functions);
    }

    private async ValueTask<byte[]> GetSpecificationAsync(AgentToolOption tool, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (specifications.TryGetValue(tool.Name, out var cached) && cached.ExpiresAt > now)
        {
            return cached.Content;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, tool.Url);
        if (string.IsNullOrEmpty(tool.SpecificationSubscriptionKey) is false)
        {
            request.Headers.Add(SubscriptionKeyHeaderName, tool.SpecificationSubscriptionKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        _ = response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        specifications[tool.Name] = new(content, now.Add(SpecificationTtl));

        return content;
    }

    private static void Authorize(HttpRequestHeaders headers, AgentToolOption tool, string? accessToken)
    {
        if (string.IsNullOrEmpty(tool.SubscriptionKey) is false)
        {
            _ = headers.Remove(SubscriptionKeyHeaderName);
            headers.Add(SubscriptionKeyHeaderName, tool.SubscriptionKey);
        }

        if (string.IsNullOrEmpty(accessToken) is false)
        {
            headers.Authorization = new("Bearer", accessToken);
        }
    }

    private sealed record class CachedSpecification(byte[] Content, DateTimeOffset ExpiresAt);
}
