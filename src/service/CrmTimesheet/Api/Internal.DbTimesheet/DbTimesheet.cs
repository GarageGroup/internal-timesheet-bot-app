using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

[DbEntity("gg_timesheetactivity", AliasName)]
internal sealed partial record class DbTimesheet : IDbEntity<DbTimesheet>
{
    private const string All = "QueryAll";

    private const string AliasName = "t";
}