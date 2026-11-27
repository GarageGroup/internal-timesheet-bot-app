using System;
using System.Net.Http;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class ApplicationHost
{
    private static IServiceCollection RegisterAgent(this IServiceCollection services)
        =>
        services
        .AddSingleton(static serviceProvider => AgentOptionReader.Read(serviceProvider.GetRequiredService<IConfiguration>()))
        .AddSingleton<IAgentUserTokenProvider, MissingAgentUserTokenProvider>()
        .AddSingleton(ResolveAgentChatFeature);

    private static AgentChatFeature ResolveAgentChatFeature(IServiceProvider serviceProvider)
    {
        var option = serviceProvider.GetRequiredService<AgentOption>();
        if (option.Enabled is false)
        {
            return new(Enabled: false, Service: new DisabledAgentChatService());
        }

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

        var chatService = new AgentChatService(
            kernelFactory: new FoundryAgentKernelFactory(option.Foundry, CreateFoundryCredential(option.Foundry)),
            toolLoader: toolLoader,
            historyStore: new TableAgentHistoryStore(
                new TableClient(option.Storage.ConnectionString, option.Storage.ConversationTableName), timeProvider),
            promptProvider: AgentPromptProvider.FromFile(option.Message.TimeZone, timeProvider),
            option: option.Message);

        return new(Enabled: true, Service: chatService);
    }

    private static TokenCredential CreateFoundryCredential(AgentFoundryOption option)
        =>
        string.IsNullOrEmpty(option.ManagedIdentityClientId)
            ? new DefaultAzureCredential()
            : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(option.ManagedIdentityClientId));
}
