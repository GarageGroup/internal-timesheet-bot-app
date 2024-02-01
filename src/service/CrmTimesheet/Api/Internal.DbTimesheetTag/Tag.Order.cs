using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetTag
{
    internal static readonly FlatArray<DbOrder> DefaultOrders
        =
        [
            new(DateFieldName, DbOrderType.Descending),
            new($"{AliasName}.createdon", DbOrderType.Descending)
        ];
}