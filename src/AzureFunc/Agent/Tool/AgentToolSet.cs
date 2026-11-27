using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentToolSet(IReadOnlyList<KernelPlugin> plugins, IReadOnlyList<IAsyncDisposable> resources) : IAsyncDisposable
{
    public IReadOnlyList<KernelPlugin> Plugins { get; } = plugins;

    public async ValueTask DisposeAsync()
    {
        foreach (var resource in resources)
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }
    }
}
