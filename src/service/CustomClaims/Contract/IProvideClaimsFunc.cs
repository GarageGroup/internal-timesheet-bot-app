using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[Endpoint(EndpointMethod.Post, "custom-claims")]
[EndpointTag("Claims")]
public interface IProvideClaimsFunc
{
    ValueTask<Result<ProvideClaimsOut, Failure<ProvideClaimsFailureCode>>> InvokeAsync(
        ProvideClaimsIn input, CancellationToken cancellationToken);
}