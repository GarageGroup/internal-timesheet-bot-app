using System;
using System.Net.Http;
using GGroupp.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static partial class Application
{
    private const string DataverseSectionName = "Dataverse";

    private static Dependency<HttpMessageHandler> UseHttpMessageHandlerStandard(string loggerCategoryName)
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler().UseLogging(loggerCategoryName);

    private static Dependency<IDataverseApiClient> UseDataverseApiClient()
        =>
        UseHttpMessageHandlerStandard("DataverseApi").UseDataverseApiClient(DataverseSectionName);

    private static Dependency<ITimesheetApi> UseTimesheetApi()
        =>
        UseDataverseApiClient().With(ResolveTimesheetApiOption).UseTimesheetApi();

    private static TimesheetApiOption ResolveTimesheetApiOption(IServiceProvider serviceProvider)
    {
        var section = serviceProvider.GetConfiguration().GetRequiredSection("TimesheetApi");

        return new TimesheetApiOption
        {
            ChannelCodes = new(
                new(TimesheetChannel.Telegram, 140120000),
                new(TimesheetChannel.Teams, 140120001),
                new(TimesheetChannel.WebChat, 140120002),
                new(TimesheetChannel.Emulator, 140120003),
                new(TimesheetChannel.Unknown, null)),

            FavoriteProjectItemsCount = section.GetValue<int?>("FavoriteProjectItemsCount"),
            FavoriteProjectDaysCount = section.GetValue<int?>("FavoriteProjectDaysCount")
        };
    }

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();
}