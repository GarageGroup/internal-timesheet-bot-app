using System.Threading.Tasks;
using GarageGroup.Infra;
using Microsoft.Extensions.Hosting;

namespace GarageGroup.Internal.Timesheet;

static class Program
{
    static Task Main()
        =>
        FunctionHost.CreateFunctionsWorkerBuilderStandard(
            useHostConfiguration: false,
            configure: Application.Configure)
        .ConfigureBotBuilder(
            storageResolver: Application.ResolveCosmosStorage)
        .Build()
        .RunAsync();
}