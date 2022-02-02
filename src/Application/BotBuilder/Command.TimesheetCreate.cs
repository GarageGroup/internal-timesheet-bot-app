using System;
using System.Collections.Generic;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GGroupp.Internal.Timesheet;

using IProjectSetSearchFunc = IAsyncValueFunc<ProjectSetSearchIn, Result<ProjectSetSearchOut, Failure<ProjectSetSearchFailureCode>>>;
using ITimesheetCreateFunc = IAsyncValueFunc<TimesheetCreateIn, Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>>;

partial class GTimesheetBotBuilder
{
    internal static IBotBuilder UseGTimesheetCreate(this IBotBuilder botBuilder, string commandName)
        =>
        botBuilder.UseTimesheetCreate(commandName, GetProjectSetSearchApi, GetTimesheetCreateApi);

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
            sp => sp.GetRequiredService<IConfiguration>().GetTimesheetCreateApiConfiguration())
        .Resolve(botContext.ServiceProvider);

    private static TimesheetCreateApiConfiguration GetTimesheetCreateApiConfiguration(this IConfiguration configuration)
        =>
        new(
            channelCodes: new Dictionary<TimesheetChannel, int?>
            {
                [TimesheetChannel.Telegram] = configuration.GetValue<int?>("DataverseChannelCodes:Telegram"),
                [TimesheetChannel.Teams] = configuration.GetValue<int?>("DataverseChannelCodes:Teams"),
                [TimesheetChannel.WebChat] = configuration.GetValue<int?>("DataverseChannelCodes:WebChat"),
                [TimesheetChannel.Emulator] = configuration.GetValue<int?>("DataverseChannelCodes:Emulator")
            });
}