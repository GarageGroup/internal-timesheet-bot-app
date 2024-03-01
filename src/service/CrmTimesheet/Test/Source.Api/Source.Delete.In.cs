using System;
using System.Collections.Generic;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static TheoryData<TimesheetDeleteIn, DataverseEntityDeleteIn> InputDeleteTestData
        =>
        new()
        {
            {
                new(Guid.Parse("17bdba90-1161-4715-b4bf-b416200acc79")),
                new("gg_timesheetactivities", new DataversePrimaryKey(Guid.Parse("17bdba90-1161-4715-b4bf-b416200acc79")))
            },
            {
                new(Guid.Parse("4835096d-03ef-4e30-abc1-77bcfe3a5d5f")),
                new("gg_timesheetactivities", new DataversePrimaryKey(Guid.Parse("4835096d-03ef-4e30-abc1-77bcfe3a5d5f")))
            },
            {
                new(Guid.Empty),
                new("gg_timesheetactivities", new DataversePrimaryKey(Guid.Empty))
            },
        };
}