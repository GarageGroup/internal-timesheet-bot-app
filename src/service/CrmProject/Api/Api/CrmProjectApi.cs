using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class CrmProjectApi<TDataverseApi> : ICrmProjectApi
    where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseSearchSupplier, IDataverseImpersonateSupplier<TDataverseApi>
{
    private static readonly FlatArray<string> ProjectTypeEntityNames;

    static CrmProjectApi()
        =>
        ProjectTypeEntityNames = new(
            ProjectJson.EntityName,
            LeadJson.EntityName,
            OpportunityJson.EntityName,
            IncidentJson.EntityName);

    private readonly TDataverseApi dataverseApi;

    private readonly ITodayProvider todayProvider;

    private readonly CrmProjectApiOption option;

    internal CrmProjectApi(TDataverseApi dataverseApi, ITodayProvider todayProvider, CrmProjectApiOption option)
    {
        this.dataverseApi = dataverseApi;
        this.todayProvider = todayProvider;
        this.option = option;
    }

    private static ProjectSetGetItem MapProjectItem(ITimesheetProjectType item)
        =>
        new(
            id: item.Id,
            name: item.Name,
            type: item.Type);

    private static ProjectSetGetFailureCode MapFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.UserNotEnabled => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.PrivilegeDenied => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.SearchableEntityNotFound => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.Throttling => ProjectSetGetFailureCode.TooManyRequests,
            _ => default
        };
}