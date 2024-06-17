using Microsoft.Extensions.DependencyInjection;
using GarageGroup.Infra;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [EndpointFunction("CustomClaimsProvider", IsSwaggerHidden = false)]
    [EndpointFunctionSecurity(FunctionAuthorizationLevel.Function)]
    internal static Dependency<ProvideClaimsEndpoint> UseCustomClaimsEndpoint()
        => 
        Dependency.From<IDataverseEntityGetSupplier>(
            ServiceProviderServiceExtensions.GetRequiredService<IDataverseApiClient>)
        .UseProvideClaimsEndpoint();
}