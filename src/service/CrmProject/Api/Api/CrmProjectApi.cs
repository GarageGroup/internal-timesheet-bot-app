using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using TDataverseApi = IDataverseImpersonateSupplier<IDataverseSearchSupplier>;
using TSqlApi = ISqlQueryEntitySetSupplier;

internal sealed partial class CrmProjectApi(TDataverseApi dataverseApi, TSqlApi sqlApi) : ICrmProjectApi
{
    private static readonly DbParameterArrayFilter AllowedProjectTypeSetFilter;

    private static readonly DbRawFilter IncidentStateCodeFilter;

    static CrmProjectApi()
    {
        AllowedProjectTypeSetFilter = DbTimesheetProject.BuildAllowedProjectTypeSetFilter();
        IncidentStateCodeFilter = DbTimesheetProject.BuildIncidentStateCodeFilter();
    }
}