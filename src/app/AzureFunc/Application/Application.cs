using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static partial class Application
{
    private const string BotEntityName = "TelegramBotRequest";

    private const string BotAuthorizationSectionName = "Bot:Authorization";

    private static IBotApi ResolveBotApi(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<BotProvider>().BotApi;

    private static IBotStorage ResolveBotStorage(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<BotProvider>().BotStorage;

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
            ServiceProviderServiceExtensions.GetRequiredService<ISqlApi>)
        .UseCrmTimesheetApi();

    private static Dependency<IUserAuthorizationApi> UseUserAuthorizationApi()
        =>
        PrimaryHandler.UseStandardSocketsHttpHandler()
        .UseLogging(
            "AzureAuthorizationApi")
        .UsePollyStandard()
        .UseHttpApi()
        .With(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>)
        .UseUserAuthorizationApi();

    private static TTimesheetCreateFlowOption ResolveTimesheetCreateFlowOptionOrThrow<TTimesheetCreateFlowOption>(IServiceProvider serviceProvider)
        where TTimesheetCreateFlowOption : class
    {
        return serviceProvider.GetConfiguration().GetRequiredSection("TimesheetEdit").Get<TTimesheetCreateFlowOption>() ?? throw CreateException();

        static InvalidOperationException CreateException()
            =>
            new("TimesheetEdit option must be specified");
    }

    private static IConfiguration GetConfiguration(this IServiceProvider serviceProvider)
        =>
        serviceProvider.GetRequiredService<IConfiguration>();
}