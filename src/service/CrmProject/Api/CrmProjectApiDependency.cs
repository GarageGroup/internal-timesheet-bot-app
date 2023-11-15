using System;
using System.Runtime.CompilerServices;
using GarageGroup.Infra;
using PrimeFuncPack;

[assembly: InternalsVisibleTo("GarageGroup.Internal.Timesheet.Service.CrmProject.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace GarageGroup.Internal.Timesheet;

public static class CrmProjectApiDependency
{
    public static Dependency<ICrmProjectApi> UseCrmProjectApi<TDataverseApi, TSqlApi>(
        this Dependency<TDataverseApi, TSqlApi> dependency)
        where TDataverseApi : IDataverseSearchSupplier, IDataverseImpersonateSupplier<IDataverseSearchSupplier>
        where TSqlApi : ISqlQueryEntitySetSupplier
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<ICrmProjectApi>(CreateApi);

        static CrmProjectApi<TDataverseApi> CreateApi(TDataverseApi dataverseApi, TSqlApi sqlApi)
        {
            ArgumentNullException.ThrowIfNull(dataverseApi);
            ArgumentNullException.ThrowIfNull(sqlApi);

            return new(dataverseApi, sqlApi);
        }
    }
}