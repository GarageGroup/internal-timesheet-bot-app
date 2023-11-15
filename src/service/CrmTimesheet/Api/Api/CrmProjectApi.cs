using System.Text.RegularExpressions;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed partial class CrmTimesheetApi<TDataverseApi> : ICrmTimesheetApi
    where TDataverseApi : IDataverseEntitySetGetSupplier, IDataverseEntityCreateSupplier, IDataverseImpersonateSupplier<TDataverseApi>
{
    private const string TagStartSymbol = "#";

    private static readonly Regex TagRegex;

    private static readonly IDbFilter DescriptionTagFilter;

    static CrmTimesheetApi()
    {
        TagRegex = CreateTagRegex();
        DescriptionTagFilter = DbTimesheetTag.BuildDescriptionFilter(TagStartSymbol);
    }

    [GeneratedRegex($"{TagStartSymbol}\\w+", RegexOptions.CultureInvariant)]
    private static partial Regex CreateTagRegex();

    private readonly TDataverseApi dataverseApi;

    private readonly ISqlQueryEntitySetSupplier sqlApi;

    private readonly CrmTimesheetApiOption option;

    internal CrmTimesheetApi(TDataverseApi dataverseApi, ISqlQueryEntitySetSupplier sqlApi, CrmTimesheetApiOption option)
    {
        this.dataverseApi = dataverseApi;
        this.sqlApi = sqlApi;
        this.option = option;
    }
}