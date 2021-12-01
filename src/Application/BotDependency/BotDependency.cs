using System;
using GGroupp.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

internal static partial class BotDependency
{
    private static Dependency<IDataverseApiClient> CreateDataverseApiDependency(string loggerCategoryName)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(
            sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger(loggerCategoryName.OrEmpty()))
        .With<IFunc<DataverseApiClientConfiguration>>(
            sp => sp.GetRequiredService<IConfiguration>().Get<DataverseClientConfigurationJson>())
        .UseDataverseApiClient();
}