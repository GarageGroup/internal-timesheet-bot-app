using System;
using System.Net.Http;
using GGroupp.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

internal static partial class BotDependency
{
    private static Dependency<LoggerDelegatingHandler> CreateStandardHttpHandlerDependency(string loggerCategoryName)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(
            sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(loggerCategoryName.OrEmpty()));

    private static Dependency<IDataverseApiClient> CreateDataverseApiClient<THttpHandler>(
        this Dependency<THttpHandler> dependency)
        where THttpHandler : HttpMessageHandler
        =>
        dependency.With<IFunc<DataverseApiClientConfiguration>>(
            sp => sp.GetRequiredService<IConfiguration>().Get<DataverseClientConfigurationJson>())
        .UseDataverseApiClient();
}