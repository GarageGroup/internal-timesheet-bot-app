using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [HttpFunction("HandleBotHttp", HttpMethodName.Post, Route = "message", AuthLevel = HttpAuthorizationLevel.Function)]
    internal static Dependency<IBotSignalHandler> UseBotSignal([DurableClient] this DurableTaskClient client)
        =>
        Dependency.Of(
            client)
        .UseOrchestrationEntityApi()
        .UseBotSignalHandler(
            BotEntityName);

    [EntityFunction("HandleBotEntity", EntityName = BotEntityName)]
    internal static Dependency<IBotWebHookHandler> UseBot()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<BotProvider>)
        .GetBotBuilder()
        .Next(
            UseCultureMiddleware())
        .UseCommands()
        .With(
            UseSignOutCommand())
        .With(
            "info", BotCommand.UseBotInfoCommand())
        .With(
            UseWelcomeCommand())
        .BuildWebHookHandler();

    private static Dependency<IChatMiddleware> UseCultureMiddleware()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IUserAuthorizationApi>)
        .UseCultureMiddleware();

    private static Dependency<IChatCommand<SignOutCommandIn, Unit>> UseSignOutCommand()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IUserAuthorizationApi>)
        .UseSignOutCommand();

    private static Dependency<IChatCommand<WelcomeCommandIn, Unit>> UseWelcomeCommand()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<WelcomeOption>)
        .UseWelcomeCommand();
}