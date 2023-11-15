using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class CrmProjectApi<TDataverseApi> : ICrmProjectApi
    where TDataverseApi : IDataverseSearchSupplier, IDataverseImpersonateSupplier<IDataverseSearchSupplier>
{
    private static readonly IDbFilter AllowedProjectTypeSetFilter;

    static CrmProjectApi()
        =>
        AllowedProjectTypeSetFilter = DbTimesheetProject.BuildAllowedProjectTypeSetFilter();

    private readonly TDataverseApi dataverseApi;

    private readonly ISqlQueryEntitySetSupplier sqlApi;

    internal CrmProjectApi(TDataverseApi dataverseApi, ISqlQueryEntitySetSupplier sqlApi)
    {
        this.dataverseApi = dataverseApi;
        this.sqlApi = sqlApi;
    }
}