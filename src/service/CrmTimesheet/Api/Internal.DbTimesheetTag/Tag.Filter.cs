using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetTag
{
    internal static DbParameterFilter BuildOwnerFilter(Guid ownerId)
        =>
        new($"{AliasName}.ownerid", DbFilterOperator.Equal, ownerId, "ownerId");

    internal static DbParameterFilter BuildProjectFilter(Guid projectId)
        =>
        new($"{AliasName}.regardingobjectid", DbFilterOperator.Equal, projectId, "projectId");

    internal static DbLikeFilter BuildDescriptionFilter(string tagStartSymbol)
        =>
        new(DescriptionFieldName, $"%{tagStartSymbol}%", "description");

    internal static DbParameterFilter BuildMinDateFilter(DateOnly minDate)
        =>
        new(DateFieldName, DbFilterOperator.GreaterOrEqual, minDate.ToString(DateFormat), "minDate");

    internal static DbParameterFilter BuildMaxDateFilter(DateOnly maxDate)
        =>
        new(DateFieldName, DbFilterOperator.LessOrEqual, maxDate.ToString(DateFormat), "maxDate");
}