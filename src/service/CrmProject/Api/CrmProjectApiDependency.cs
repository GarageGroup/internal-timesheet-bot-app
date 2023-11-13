using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.CrmProject.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class CrmProjectApiDependency
{
    public static Dependency<ICrmProjectApi> UseCrmProjectApi<TDataverseApi>(this Dependency<TDataverseApi, CrmProjectApiOption> dependency)
        where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseSearchSupplier, IDataverseImpersonateSupplier<TDataverseApi>
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICrmProjectApi>(CreateApi);

        static CrmProjectApi<TDataverseApi> CreateApi(TDataverseApi dataverseApi, CrmProjectApiOption option)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new(dataverseApi, TodayProvider.Instance, option);
        }
    }
}