using GarageGroup.Infra;
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
            Dependency.From(ServiceProviderServiceExtensions.GetRequiredService<ISqlApi>).UseServiceHealthCheckApi("DataverseDb"),
            Dependency.From(ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>).UseServiceHealthCheckApi("DataverseApi"))
        .UseHealthCheckHandler();
}