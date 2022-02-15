using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace GGroupp.Internal.Timesheet;

static class Program
{
    static async Task Main(string[] args)
        =>
        await RunAsync(args);

    private static Task RunAsync(string[] args)
        =>
        Host.CreateDefaultBuilder(args)
        .ConfigureSocketsHttpHandlerProvider()
        .ConfigureBotBuilder(
            GTimesheetBotBuilder.ResolveCosmosStorage)
        .ConfigureBotWebHostDefaults(
            ConfigureGTimesheetBot)
        .Build()
        .RunAsync();

    private static IBotBuilder ConfigureGTimesheetBot(IBotBuilder bot)
        =>
        bot.UseLogout("logout")
        .UseGTimesheetBotStop("stop")
        .UseGTimesheetAuthorization()
        .UseGTimesheetBotInfo("info")
        .UseGTimesheetCreate("newts")
        .UseGTimesheetSetGet("showts");
}