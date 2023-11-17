using System.Text.RegularExpressions;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using TDataverseApi = IDataverseImpersonateSupplier<IDataverseEntityCreateSupplier>;
using TSqlApi = ISqlQueryEntitySetSupplier;

internal sealed partial class CrmTimesheetApi(TDataverseApi dataverseApi, TSqlApi sqlApi, CrmTimesheetApiOption option) : ICrmTimesheetApi
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
}