using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheet
{
    internal static readonly FlatArray<DbOrder> DefaultOrders
        =
        new DbOrder($"{AliasName}.createdon", DbOrderType.Ascending).AsFlatArray();
}