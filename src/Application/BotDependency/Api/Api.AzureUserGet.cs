using System;
using System.Threading;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;

partial class BotDependency
{
    private static readonly Lazy<Dependency<IAzureUserGetFunc>> azureUserGetApiDependency
        =
        new(CreateAzureUserGetDependency, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static IAzureUserGetFunc GetAzureUserGetApi(IBotContext botContext)
        =>
        azureUserGetApiDependency.Value.Resolve(botContext.ServiceProvider);

    private static Dependency<IAzureUserGetFunc> CreateAzureUserGetDependency()
        =>
        CreateStandardHttpHandlerDependency("AzureUserGetApi")
        .UseAzureUserMeGetApi(
            sp => sp.GetRequiredService<IConfiguration>().Get<AzureUserApiConfigurationJson>());
}