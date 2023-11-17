using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using TDataverseApi = IDataverseImpersonateSupplier<IDataverseSearchSupplier>;
using TSqlApi = ISqlQueryEntitySetSupplier;

internal sealed partial class CrmProjectApi(TDataverseApi dataverseApi, TSqlApi sqlApi) : ICrmProjectApi
{
    private static readonly IDbFilter AllowedProjectTypeSetFilter;

    static CrmProjectApi()
        =>
        AllowedProjectTypeSetFilter = DbTimesheetProject.BuildAllowedProjectTypeSetFilter();
}