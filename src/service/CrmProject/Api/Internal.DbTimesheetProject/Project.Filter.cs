using System;
using System.Linq;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetProject
{
    internal static DbRawFilter BuildIncidentStateCodeFilter()
        =>
        new($"({AliasName}.regardingobjecttypecode <> {TimesheetProjectType.Incident:D} " +
            $"OR EXISTS (SELECT TOP 1 1 FROM incident AS i WHERE {AliasName}.regardingobjectid = i.incidentid AND i.statecode = 0))");

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
        new($"{AliasName}.gg_date", DbFilterOperator.GreaterOrEqual, minDate.ToString("yyyy-MM-dd"), "minDate");
}