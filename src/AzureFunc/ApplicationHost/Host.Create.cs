using System;
using GarageGroup.Infra;
using System.Net.Http;
using GarageGroup.Infra.Telegram.Bot;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class ApplicationHost
{
    internal static IHostBuilder CreateBuilder()
        =>
        FunctionHost.CreateFunctionsWorkerBuilderStandard(
            useHostConfiguration: false,
            configure: Configure);

    private static void Configure(IFunctionsWorkerApplicationBuilder builder)
        =>
        builder.Services
        .RegisterBotProvider()
        .RegisterDataverseApi()
        .RegisterUserAuthorizationApi()
        .RegisterAgentApi()
        .RegisterWelcomeOption();

    private static IServiceCollection RegisterAgentApi(this IServiceCollection services)
    {
        _ = services.AddSingleton<IAgentVoiceFileApi>(static serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiKey = configuration["TelegramBot:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("TelegramBot:ApiKey must be specified.");
            }

            var client = new HttpClient(new SocketsHttpHandler())
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            return new AgentVoiceFileApi(client);
        });
        _ = services.AddSingleton(ResolveAgentVoiceOption);

        _ = services.AddSingleton(static serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var clientId = configuration["AgentApi:ManagedIdentityClientId"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("AgentApi:ManagedIdentityClientId must be configured.");
            }

            return new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId));
        });

        _ = services.AddTransient(static serviceProvider => new AgentAccessTokenHandler(
            serviceProvider.GetRequiredService<ManagedIdentityCredential>(),
            serviceProvider.GetRequiredService<IConfiguration>()));

        _ = services.AddHttpClient<IAgentMessageApi, AgentMessageApi>(static (serviceProvider, client) =>
            ConfigureAgentApiClient(serviceProvider, client))
        .AddHttpMessageHandler<AgentAccessTokenHandler>();

        _ = services.AddHttpClient<IAgentActionApi, AgentActionApi>(static (serviceProvider, client) =>
            ConfigureAgentApiClient(serviceProvider, client))
        .AddHttpMessageHandler<AgentAccessTokenHandler>();

        return services;

        static void ConfigureAgentApiClient(IServiceProvider serviceProvider, HttpClient client)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseAddress = configuration["AgentApi:BaseAddress"];

            if (Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri) is false)
            {
                throw new InvalidOperationException("AgentApi:BaseAddress must be an absolute URI.");
            }

            client.BaseAddress = uri;
            client.Timeout = TimeSpan.FromSeconds(60);
        }

        static AgentVoiceOption ResolveAgentVoiceOption(IServiceProvider serviceProvider)
        {
            var maxFileSizeBytes = serviceProvider.GetRequiredService<IConfiguration>().GetValue(
                "AgentVoice:MaxFileSizeBytes",
                5 * 1024 * 1024);

            if (maxFileSizeBytes <= 0)
            {
                throw new InvalidOperationException("AgentVoice:MaxFileSizeBytes must be positive.");
            }

            return new(maxFileSizeBytes);
        }
    }

    private static IServiceCollection RegisterBotProvider(this IServiceCollection services)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("BotApi")
        .UsePollyStandard()
        .ConfigureHttpHeader("Ocp-Apim-Subscription-Key", "TelegramBot:ApiKey")
        .UseHttpApi("TelegramBot")
        .UseTelegramBotApi()
        .With<IBotStorage>(InMemoryBotStorage.Instance)
        .UseBotProvider("Bot")
        .ToRegistrar(services)
        .RegisterSingleton();

    private static IServiceCollection RegisterDataverseApi(this IServiceCollection services)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("DataverseApi")
        .UseTokenCredentialStandard()
        .UsePollyStandard()
        .UseDataverseApiClient("Dataverse")
        .ToRegistrar(services)
        .RegisterScoped();

    private static IServiceCollection RegisterUserAuthorizationApi(this IServiceCollection services)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging("AzureAuthorizationApi")
        .UsePollyStandard()
        .UseHttpApi()
        .With(ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>)
        .UseUserAuthorizationApi()
        .ToRegistrar(services)
        .RegisterScoped();

    private static IServiceCollection RegisterWelcomeOption(this IServiceCollection services)
        =>
        services.AddSingleton(ResolveWelcomeOption);

    private static WelcomeOption ResolveWelcomeOption(IServiceProvider serviceProvider)
    {
        var section = serviceProvider.GetRequiredService<IConfiguration>().GetRequiredSection("Welcome");

        return new(
            imageUrl: section["ImageUrl"].OrEmpty())
        {
            Emojis = section.GetSection("Emojis").Get<string[]>()
        };
    }
}
