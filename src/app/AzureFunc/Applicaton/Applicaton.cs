using System;
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

    private static readonly CrmTimesheetApiOption TimesheetApiOption
        =
        new(
            channelCodes: new(
                new(TimesheetChannel.Telegram, 140120000),
                new(TimesheetChannel.Teams, 140120001),
                new(TimesheetChannel.WebChat, 140120002),
                new(TimesheetChannel.Emulator, 140120003),
                new(TimesheetChannel.Unknown, null)));

    private static Dependency<HttpMessageHandler> UseHttpMessageHandlerStandard(string loggerCategoryName)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(loggerCategoryName)
        .UsePollyStandard(HttpStatusCode.TooManyRequests);

    private static Dependency<ICrmProjectApi> UseCrmProjectApi()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>,
            ResolveProjectApiOption)
        .UseCrmProjectApi();

    private static Dependency<ICrmTimesheetApi> UseCrmTimesheetApi()
        =>
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>)
        .With(
            TimesheetApiOption)
        .UseCrmTimesheetApi();

    private static CrmProjectApiOption ResolveProjectApiOption(IServiceProvider serviceProvider)
    {
        var section = serviceProvider.GetConfiguration().GetRequiredSection("CrmProjectApi");

        return new()
        {
            LastProjectItemsCount = section.GetValue<int?>("LastProjectItemsCount"),
            LastProjectDaysCount = section.GetValue<int?>("LastProjectDaysCount")
        };
    }

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();
}