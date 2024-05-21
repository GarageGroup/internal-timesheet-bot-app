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
        this Dependency<TDataverseApi, TSqlApi> dependency)
        where TDataverseApi : IDataverseApiClient
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICrmTimesheetApi>(CreateApi);

        static CrmTimesheetApi CreateApi(TDataverseApi dataverseApi, TSqlApi sqlApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(sqlApi);

            return new(dataverseApi, sqlApi);
        }
    }
}