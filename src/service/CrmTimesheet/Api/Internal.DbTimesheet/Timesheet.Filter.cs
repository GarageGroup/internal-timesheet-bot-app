using System;
using System.Linq;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    internal static DbParameterArrayFilter BuildAllowedProjectTypeSetFilter()
    {
        return new(
            fieldName: $"{AliasName}.regardingobjecttypecode",
            @operator: DbArrayFilterOperator.In,
            fieldValues: Enum.GetValues<TimesheetProjectType>().Select(AsInt32).OrderBy(Pipeline.Pipe).Select(AsObject).ToFlatArray(),
            parameterPrefix: "projectTypeCode");

        static int AsInt32(TimesheetProjectType type)
            =>
            (int)type;

        static object? AsObject(int type)
            =>
            type;
    }

    internal static DbParameterFilter BuildOwnerFilter(Guid ownerId)
        =>
        new($"{AliasName}.ownerid", DbFilterOperator.Equal, ownerId, "ownerId");

    internal static DbParameterFilter BuildDateFilter(DateOnly date)
        =>
        new($"{AliasName}.gg_date", DbFilterOperator.Equal, date.ToString("yyyy-MM-dd"), "date");
}