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
            serviceUrl: DataverseApiServiceUrl.OrEmpty(),
            authTenantId: DataverseApiAuthTenantId.OrEmpty(),
            authClientId: DataverseApiAuthClientId.OrEmpty(),
            authClientSecret: DataverseApiAuthClientSecret.OrEmpty());
}