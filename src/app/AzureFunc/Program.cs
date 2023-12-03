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
            static c => c.AddJsonFile("appsettings.json", optional: false))
        .ConfigureFunctionsWorkerStandard(
            useHostConfiguration: false,
            configure: Application.Configure)
        .ConfigureBotBuilder(
            storageResolver: Application.ResolveCosmosStorage)
        .Build()
        .RunAsync();
}