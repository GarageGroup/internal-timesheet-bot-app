using System;
using System.Collections.Generic;
using GarageGroup.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static partial class Application
{
    private const string BotEntityName = "BotRequest";

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
            ServiceProviderServiceExtensions.GetRequiredService<ISqlApi>,
            ResolveTimesheetApiOption)
        .UseCrmTimesheetApi();

    private static CrmTimesheetApiOption ResolveTimesheetApiOption(IServiceProvider serviceProvider)
    {
        var section = serviceProvider.GetConfiguration().GetRequiredSection("CrmTimesheetApi:ChannelCodes");

        return new(
            channelCodes: section.Get<Dictionary<TimesheetChannel, int?>>().ToFlatArray());
    }

    private static TTimesheetEditOption ResolveTimesheetEditOptionOrThrow<TTimesheetEditOption>(IServiceProvider serviceProvider)
        where TTimesheetEditOption : class
    {
        return serviceProvider.GetConfiguration().GetRequiredSection("TimesheetEdit").Get<TTimesheetEditOption>() ?? throw CreateException();

        static InvalidOperationException CreateException()
            =>
            new("TimesheetEditOption must be specified");
    }

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();
}