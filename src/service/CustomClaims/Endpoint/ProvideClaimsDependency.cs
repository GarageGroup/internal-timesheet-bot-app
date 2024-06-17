using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.CustomClaims.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class ProvideClaimsDependency
{
    public static Dependency<ProvideClaimsEndpoint> UseProvideClaimsEndpoint(this Dependency<IDataverseEntityGetSupplier> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map(CreateFunc).Map(ProvideClaimsEndpoint.Resolve);

        static ProvideClaimsFunc CreateFunc(IDataverseEntityGetSupplier dataverseApi)
        {
            return new ProvideClaimsFunc(dataverseApi);
        }
    }
}