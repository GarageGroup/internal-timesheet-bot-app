using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class CrmTimesheetApiDependency
{
    public static Dependency<ICrmTimesheetApi> UseCrmTimesheetApi<TDataverseApi>(this Dependency<TDataverseApi, CrmTimesheetApiOption> dependency)
        where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseEntityCreateSupplier, IDataverseImpersonateSupplier<TDataverseApi>
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICrmTimesheetApi>(CreateApi);

        static CrmTimesheetApi<TDataverseApi> CreateApi(TDataverseApi dataverseApi, CrmTimesheetApiOption option)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            return new(dataverseApi, option);
        }
    }
}