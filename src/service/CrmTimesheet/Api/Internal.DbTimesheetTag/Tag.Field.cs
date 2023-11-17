using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetTag
{
    [DbSelect(All, AliasName, DescriptionFieldName)]
    public string? Description { get; init; }
}