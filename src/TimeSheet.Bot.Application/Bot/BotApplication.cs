#nullable enable

using System.Threading.Tasks;
using GGroupp.Infra;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal static class BotApplication
    {
        public static Task RunAsync(string[] args)
            =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(
                b => b.ConfigureSocketsHandlerProvider().Configure(Configure))
            .Build()
            .RunAsync();

        private static IWebHostBuilder ConfigureSocketsHandlerProvider(this IWebHostBuilder builder)
            =>
            builder.ConfigureServices(
                s => s.AddSingleton<ISocketsHttpHandlerProvider, DefaultSocketsHttpHandlerProvider>());

        private static void Configure(IApplicationBuilder app)
            =>
            app
            .UseWebSockets()
            .UseAuthorization(
                _ => new())
            .UseTimesheetBot();
    }
}