﻿using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace GarageGroup.Internal.Timesheet;

static class Program
{
    static Task Main()
        =>
        Host.CreateDefaultBuilder()
        .ConfigureFunctionsWorkerStandard()
        .ConfigureBotBuilder(Application.ResolveCosmosStorage)
        .Build()
        .RunAsync();
}