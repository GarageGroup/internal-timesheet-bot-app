using System;
using System.Collections.Generic;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GGroupp.Internal.Timesheet;

using IFavoriteProjectSetGetFunc = IAsyncValueFunc<FavoriteProjectSetGetIn, Result<FavoriteProjectSetGetOut, Failure<FavoriteProjectSetGetFailureCode>>>;
using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetCreate(this IBotBuilder botBuilder)
        =>
        botBuilder.UseTimesheetCreate(TimesheetCreateCommand, GetFavoriteProjectSetGetApi, GetProjectSetSearchApi, GetTimesheetCreateApi);

    private static IFavoriteProjectSetGetFunc GetFavoriteProjectSetGetApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("FavoriteProjectSetGetApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseFavoriteProjectSetGetApi(
            sp => sp.GetRequiredService<IConfiguration>().GetFavoriteProjectSetGetApiOption())
        .Resolve(botContext.ServiceProvider);

    private static IProjectSetSearchFunc GetProjectSetSearchApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("ProjectSetSearchApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseProjectSetSearchApi()
        .Resolve(botContext.ServiceProvider);

    private static ITimesheetCreateFunc GetTimesheetCreateApi(IBotContext botContext)
        =>
        CreateStandardHttpHandlerDependency("TimesheetCreateApi")
        .UseDataverseImpersonation(botContext)
        .CreateDataverseApiClient()
        .UseTimesheetCreateApi(
            sp => sp.GetRequiredService<IConfiguration>().GetTimesheetCreateApiOption())
        .Resolve(botContext.ServiceProvider);

    private static TimesheetCreateApiOption GetTimesheetCreateApiOption(this IConfiguration configuration)
        =>
        new(
            channelCodes: new Dictionary<TimesheetChannel, int?>
            {
                [TimesheetChannel.Telegram] = configuration.GetValue<int?>("DataverseChannelCodes:Telegram"),
                [TimesheetChannel.Teams] = configuration.GetValue<int?>("DataverseChannelCodes:Teams"),
                [TimesheetChannel.WebChat] = configuration.GetValue<int?>("DataverseChannelCodes:WebChat"),
                [TimesheetChannel.Emulator] = configuration.GetValue<int?>("DataverseChannelCodes:Emulator")
            });

    private static FavoriteProjectSetGetApiOption GetFavoriteProjectSetGetApiOption(this IConfiguration configuration)
        =>
        new(
            countTimesheetItems: configuration.GetValue<int?>("CountTimesheetItems"),
            countTimesheetDays: configuration.GetValue<int?>("CountTimesheetDays"));
}