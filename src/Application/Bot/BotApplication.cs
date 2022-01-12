using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Hosting;
using static GGroupp.Internal.Timesheet.BotDependency;

namespace GGroupp.Internal.Timesheet;

internal static class BotApplication
{
    internal static Task RunAsync(string[] args)
        =>
        Host.CreateDefaultBuilder(args)
        .ConfigureSocketsHttpHandlerProvider()
        .ConfigureBotBuilder(
            ResolveCosmosStorage)
        .ConfigureBotWebHostDefaults(
            ConfigureGTimesheetBot)
        .Build()
        .RunAsync();

    private static IBotBuilder ConfigureGTimesheetBot(IBotBuilder bot)
        =>
        bot
        .UseUserLogOut(
            _ => new("logout"))
        .UseConversationCancel(
            _ => new("cancel", successText: "Операция была отменена"))
        .UseAuthorization(
            GetAzureUserGetApi,
            GetDataverseUserGetApi,
            GetUserAuthorizeConfigurationProvider)
        .UseBotInfoGet(
            _ => new("info", helloText: "Привет! Это G-Timesheet бот!"))
        .UseTimesheetCreate(
            _ => new("newtimesheet"),
            GetProjectSetSearchApi,
            GetTimesheetCreateApi);
}