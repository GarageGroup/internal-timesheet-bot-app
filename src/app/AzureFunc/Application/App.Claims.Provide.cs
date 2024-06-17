using Microsoft.Extensions.DependencyInjection;
using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointFunction("ClaimsProvider", IsSwaggerHidden = true)]
    [EndpointFunctionSecurity(FunctionAuthorizationLevel.Function)]
    internal static Dependency<ClaimsProvideEndpoint> UseClaimsProvideEndpoint()
        => 
        Dependency.From(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>)
        .UseClaimsProvideEndpoint();
}