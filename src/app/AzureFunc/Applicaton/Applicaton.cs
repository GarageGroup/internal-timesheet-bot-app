using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using GarageGroup.Infra;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

[HealthCheckFunc("HealthCheck", AuthLevel = AuthorizationLevel.Function)]
internal static partial class Application
{
    private const string DataverseSectionName = "Dataverse";

    private static Dependency<HttpMessageHandler> UseHttpMessageHandlerStandard(string loggerCategoryName)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(loggerCategoryName)
        .UsePollyStandard(HttpStatusCode.TooManyRequests);

    private static Dependency<ICrmProjectApi> UseCrmProjectApi()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>,
            ServiceProviderServiceExtensions.GetRequiredService<ISqlApi>)
        .UseCrmProjectApi();

    private static Dependency<ICrmTimesheetApi> UseCrmTimesheetApi()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>,
            ResolveTimesheetApiOption)
        .UseCrmTimesheetApi();

    private static CrmTimesheetApiOption ResolveTimesheetApiOption(IServiceProvider serviceProvider)
    {
        var section = serviceProvider.GetConfiguration().GetRequiredSection("CrmTimesheetApi:ChannelCodes");

        return new(
            channelCodes: section.Get<Dictionary<TimesheetChannel, int?>>().ToFlatArray());
    }

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();
}