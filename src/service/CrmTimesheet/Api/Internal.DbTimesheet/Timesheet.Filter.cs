using System;
using System.Linq;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    internal static readonly DbParameterArrayFilter AllowedProjectTypeSetFilter
        =
        new(
            fieldName: $"{AliasName}.regardingobjecttypecode",
            @operator: DbArrayFilterOperator.In,
            fieldValues: Enum.GetValues<TimesheetProjectType>().Select(AsInt32).OrderBy(Pipeline.Pipe).Select(AsObject).ToFlatArray(),
            parameterPrefix: "projectTypeCode");

    internal static DbParameterFilter BuildOwnerFilter(Guid ownerId)
        =>
        new($"{AliasName}.ownerid", DbFilterOperator.Equal, ownerId, "ownerId");

    internal static DbParameterFilter BuildDateFilter(DateOnly date)
        =>
        new($"{AliasName}.gg_date", DbFilterOperator.Equal, date.ToString("yyyy-MM-dd"), "date");

    private static int AsInt32(TimesheetProjectType type)
        =>
        (int)type;

    private static object? AsObject(int type)
        =>
        type;
}