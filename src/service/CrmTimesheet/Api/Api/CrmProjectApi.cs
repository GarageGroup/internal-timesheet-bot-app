using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class CrmTimesheetApi<TDataverseApi> : ICrmTimesheetApi
    where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseEntityCreateSupplier, IDataverseImpersonateSupplier<TDataverseApi>
{
    private readonly TDataverseApi dataverseApi;

    private readonly CrmTimesheetApiOption option;

    internal CrmTimesheetApi(TDataverseApi dataverseApi, CrmTimesheetApiOption option)
    {
        this.dataverseApi = dataverseApi;
        this.option = option;
    }
}