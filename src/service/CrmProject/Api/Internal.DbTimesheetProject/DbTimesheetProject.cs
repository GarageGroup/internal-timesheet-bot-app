using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[DbEntity("gg_timesheetactivity", AliasName)]
internal sealed partial record class DbTimesheetProject : IDbEntity<DbTimesheetProject>
{
    private const string All = "QueryAll";

    private const string AliasName = "t";

    private const string ProjectTypeCodeFieldName = $"{AliasName}.regardingobjecttypecode";
}