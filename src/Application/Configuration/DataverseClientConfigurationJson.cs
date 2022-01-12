using System;
using GGroupp.Infra;

namespace GGroupp.Internal.Timesheet;

internal sealed record class DataverseClientConfigurationJson : IFunc<DataverseApiClientConfiguration>
{
    public string? DataverseApiServiceUrl { get; init; }

    public string? DataverseApiAuthTenantId { get; init; }

    public string? DataverseApiAuthClientId { get; init; }

    public string? DataverseApiAuthClientSecret { get; init; }

    DataverseApiClientConfiguration IFunc<DataverseApiClientConfiguration>.Invoke()
        =>
        new(
            serviceUrl: DataverseApiServiceUrl ?? string.Empty,
            authTenantId: DataverseApiAuthTenantId ?? string.Empty,
            authClientId: DataverseApiAuthClientId ?? string.Empty,
            authClientSecret: DataverseApiAuthClientSecret ?? string.Empty);
}