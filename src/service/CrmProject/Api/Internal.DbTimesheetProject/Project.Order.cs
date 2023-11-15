using System;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial record class DbTimesheetProject
{
    internal static readonly FlatArray<DbOrder> DefaultOrders
        =
        new(
            new(nameof(MaxDate), DbOrderType.Descending),
            new(nameof(MaxCreatedOn), DbOrderType.Descending));
}