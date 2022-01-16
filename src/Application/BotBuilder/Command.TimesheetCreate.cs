using System;
using System.Threading;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;
using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetAuthorization(this IBotBuilder botBuilder)
        =>
        botBuilder.UseDataverseAuthorization(
            AuthorizationBotBuilder.ResolveStandardOption,
            GetAzureUserGetApi,
            GetDataverseUserGetApi);

    private static IAzureUserGetFunc GetAzureUserGetApi(IBotContext botContext)
        =>
        azureUserGetApiDependency.Value.Resolve(botContext.ServiceProvider);

    private static IDataverseUserGetFunc GetDataverseUserGetApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("DataverseUserGetApi")
        .CreateDataverseApiClient()
        .UseUserGetApi()
        .Resolve(botContext.ServiceProvider);

    private static readonly Lazy<Dependency<IAzureUserGetFunc>> azureUserGetApiDependency
        =
        new(CreateAzureUserGetDependency, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Dependency<IAzureUserGetFunc> CreateAzureUserGetDependency()
        =>
        CreateStandardHttpHandlerDependency("AzureUserGetApi")
        .UseAzureUserMeGetApi(
            sp => sp.GetRequiredService<IConfiguration>().Get<AzureUserApiConfigurationJson>());
}