using GarageGroup.Infra;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [HttpFunction("HandleProactiveMessage", HttpMethodName.Post, Route = "proactiveMessage", AuthLevel = HttpAuthorizationLevel.Function)]
    internal static Dependency<IProactiveMessageHandler> UseProactiveMessageHandler()
        =>
        Dependency.From(
            StandardCloudAdapter.Resolve,
            ServiceProviderServiceExtensions.GetRequiredService<ICosmosStorage>)
        .UseProactiveMessageHandler();
}