using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [HttpFunction("HealthCheck", HttpMethodName.Get, Route = "health", AuthLevel = HttpAuthorizationLevel.Function)]
    internal static Dependency<IHealthCheckHandler> UseHealthCheck()
        =>
        HealthCheck.UseServices(
            Dependency.From(ResolveBotApi).UseServiceHealthCheckApi("TelegramBotApi"),
            Dependency.From(ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>).UseServiceHealthCheckApi("DataverseApi"))
        .UseHealthCheckHandler();

    private static IBotApi ResolveBotApi(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<BotProvider>().BotApi;
}