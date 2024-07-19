using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Telegram.Bot;
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
        .RegisterWelcomeOption();

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