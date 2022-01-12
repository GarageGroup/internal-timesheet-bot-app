using System;
using GGroupp.Platform;

namespace GGroupp.Internal.Timesheet;

internal sealed class AzureUserApiConfigurationJson : IFunc<AzureUserApiConfiguration>
{
    public string? GraphApiBaseAddressUrl { get; init; }

    AzureUserApiConfiguration IFunc<AzureUserApiConfiguration>.Invoke()
        =>
        new(
            graphApiBaseAddress: new(GraphApiBaseAddressUrl ?? string.Empty));
}