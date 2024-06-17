using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[Endpoint(EndpointMethod.Post, "provide-claims")]
[EndpointTag("Claims")]
public interface IClaimsProvideFunc
{
    ValueTask<Result<ClaimsProvideOut, Failure<ClaimsProvideFailureCode>>> InvokeAsync(
        ClaimsProvideIn input, CancellationToken cancellationToken);
}