using System;
using System.Collections.Generic;
using System.Net.Http;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace GarageGroup.Internal.Timesheet;

partial class ApplicationHost
{
    private static IServiceCollection RegisterAgent(this IServiceCollection services)
        =>
        services
        .AddSingleton(static serviceProvider => AgentOptionReader.Read(serviceProvider.GetRequiredService<IConfiguration>()))
        .AddSingleton(ResolveAgentSignInRuntime)
        .AddSingleton<IAgentUserTokenProvider>(
            static serviceProvider => serviceProvider.GetRequiredService<AgentSignInRuntime>().Service is AgentSignInService service
                ? service
                : new MissingAgentUserTokenProvider())
        .AddSingleton(ResolveAgentChatFeature);

    private static AgentSignInRuntime ResolveAgentSignInRuntime(IServiceProvider serviceProvider)
    {
        var option = serviceProvider.GetRequiredService<AgentOption>();
        var signInAudience = Array.Find(option.Tools, static tool => string.IsNullOrEmpty(tool.Audience) is false)?.Audience;

        if (option.SignIn is not AgentSignInOption signIn || signInAudience is null)
        {
            return new(Service: null);
        }

        if (string.IsNullOrEmpty(signIn.ManagedIdentityClientId))
        {
            throw new InvalidOperationException("Agent:SignIn:ManagedIdentityClientId or Agent:Foundry:ManagedIdentityClientId must be specified.");
        }

        var credential = new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(signIn.ManagedIdentityClientId));

        var tokenClient = new EntraTokenClient(
            httpClient: new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
            {
                Timeout = TimeSpan.FromSeconds(30)
            },
            option: signIn,
            clientAssertionProvider: async cancellationToken =>
            {
                var assertion = await credential.GetTokenAsync(new([EntraTokenClient.ClientAssertionScope]), cancellationToken).ConfigureAwait(false);
                return assertion.Token;
            });

        var service = new AgentSignInService(
            option: signIn,
            signInAudience: signInAudience,
            store: new TableAgentSignInStore(new TableClient(option.Storage.ConnectionString, signIn.TableName)),
            tokenClient: tokenClient,
            protector: new AesGcmAgentTokenProtector(signIn.TokenEncryptionKey),
            timeProvider: TimeProvider.System,
            logger: serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<AgentSignInService>());

        return new(service);
    }

    private static AgentChatFeature ResolveAgentChatFeature(IServiceProvider serviceProvider)
    {
        var option = serviceProvider.GetRequiredService<AgentOption>();
        if (string.IsNullOrWhiteSpace(option.Storage.ConnectionString))
        {
            throw new InvalidOperationException("Agent:Storage:ConnectionString or AzureWebJobsStorage must be specified.");
        }

        var timeProvider = TimeProvider.System;

        var toolLoader = new AgentToolLoader(
            tools: option.Tools,
            primaryHandler: new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            },
            tokenProvider: serviceProvider.GetRequiredService<IAgentUserTokenProvider>(),
            loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>(),
            timeProvider: timeProvider);

        var confirmationFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tool in option.Tools)
        {
            foreach (var operation in tool.ConfirmationOperations)
            {
                _ = confirmationFunctions.Add(AgentToolInvocationFilter.GetKey(tool.Name, operation));
            }
        }

        var foundryCredential = CreateFoundryCredential(option.Foundry);

        var chatService = new AgentChatService(
            kernelFactory: new FoundryAgentKernelFactory(option.Foundry, foundryCredential),
            toolLoader: toolLoader,
            historyStore: new TableAgentHistoryStore(
                new TableClient(option.Storage.ConnectionString, option.Storage.ConversationTableName), timeProvider),
            actionStore: new TableAgentActionStore(
                new TableClient(option.Storage.ConnectionString, option.Storage.ActionTableName)),
            entityCache: new AgentEntityCache(),
            promptProvider: AgentPromptProvider.FromFile(option.Message.TimeZone, timeProvider),
            option: new(option.Message, confirmationFunctions, option.Message.ActionTtl),
            timeProvider: timeProvider,
            logger: serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<AgentChatService>());

#pragma warning disable SKEXP0010
        var voiceTranscriber = option.Voice is null
            ? null
            : new AgentVoiceTranscriber(
                new AzureOpenAIAudioToTextService(
                    option.Voice.DeploymentName, option.Voice.Endpoint.AbsoluteUri, foundryCredential, option.Voice.ModelId));
#pragma warning restore SKEXP0010

        return new(chatService)
        {
            SignIn = serviceProvider.GetRequiredService<AgentSignInRuntime>().Service,
            Voice = voiceTranscriber
        };
    }

    private static TokenCredential CreateFoundryCredential(AgentFoundryOption option)
        =>
        string.IsNullOrEmpty(option.ManagedIdentityClientId)
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(option.ManagedIdentityClientId));
}
