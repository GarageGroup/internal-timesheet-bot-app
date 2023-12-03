using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GarageGroup.Internal.Timesheet;

static class Program
{
    static Task Main()
        =>
        Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(
            static (context, builder) => builder.AddJsonFile(
                Path.Combine(context.HostingEnvironment.ContentRootPath, "appsettings.json"), optional: false))
        .ConfigureFunctionsWorkerStandard(
            useHostConfiguration: false,
            configure: Application.Configure)
        .ConfigureBotBuilder(
            storageResolver: Application.ResolveCosmosStorage)
        .Build()
        .RunAsync();
}