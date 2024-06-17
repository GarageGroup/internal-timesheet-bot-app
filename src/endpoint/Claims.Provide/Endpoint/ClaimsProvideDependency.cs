using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Claims.Provide.Test")]

namespace GarageGroup.Internal.Timesheet;

public static class ClaimsProvideDependency
{
    public static Dependency<ClaimsProvideEndpoint> UseClaimsProvideEndpoint<TDataverseApi>(this Dependency<TDataverseApi> dependency) 
        where TDataverseApi : IDataverseEntityGetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);

        return dependency.Map(CreateFunc).Map(ClaimsProvideEndpoint.Resolve);

        static ClaimsProvideFunc CreateFunc(TDataverseApi dataverseApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new ClaimsProvideFunc(dataverseApi);
        }
    }
}
