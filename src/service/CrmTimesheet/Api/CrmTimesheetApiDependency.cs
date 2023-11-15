using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class CrmTimesheetApiDependency
{
    public static Dependency<ICrmTimesheetApi> UseCrmTimesheetApi<TDataverseApi, TSqlApi>(
        this Dependency<TDataverseApi, TSqlApi, CrmTimesheetApiOption> dependency)
        where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseEntityCreateSupplier, IDataverseImpersonateSupplier<TDataverseApi>
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICrmTimesheetApi>(CreateApi);

        static CrmTimesheetApi<TDataverseApi> CreateApi(TDataverseApi dataverseApi, TSqlApi sqlApi, CrmTimesheetApiOption option)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(sqlApi);

            return new(dataverseApi, sqlApi, option);
        }
    }
}