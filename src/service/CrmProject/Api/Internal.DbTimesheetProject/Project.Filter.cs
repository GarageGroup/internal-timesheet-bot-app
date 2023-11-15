using System;
using System.Linq;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetProject
{
    internal static DbParameterArrayFilter BuildAllowedProjectTypeSetFilter()
    {
        return new(
            fieldName: ProjectTypeCodeFieldName,
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

    internal static DbParameterFilter BuildMinDateFilter(DateOnly minDate)
        =>
        new($"{AliasName}.gg_date", DbFilterOperator.Greater, minDate.ToString("yyyy-MM-dd"), "minDate");
}