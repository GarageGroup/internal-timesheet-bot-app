using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
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
            () => new MemoryStorage())
        .ConfigureBotWebHostDefaults(
            ConfigureGTimesheetBot)
        .Build()
        .RunAsync();

    private static IBotBuilder ConfigureGTimesheetBot(IBotBuilder bot)
        =>
        bot
        .UseBotStart()
        .UseTimesheetCreate(
            ResolveProjectSetSearchApi,
            ResolveTimesheetCreateApi);
}