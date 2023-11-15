using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    internal static DbParameterFilter BuildOwnerFilter(Guid ownerId)
        =>
        new($"{AliasName}.ownerid", DbFilterOperator.Equal, ownerId, "ownerId");

    internal static DbParameterFilter BuildDateFilter(DateOnly date)
        =>
        new($"{AliasName}.gg_date", DbFilterOperator.Equal, date.ToString("yyyy-MM-dd"), "date");
}