using System;
using System.Collections.Generic;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

using IEntityDictionary = IReadOnlyDictionary<string, object?>;

partial class CrmTimesheetApiSource
{
    public static TheoryData<TimesheetUpdateIn, DataverseEntityUpdateIn<IEntityDictionary>> InputUpdateTestData
        =>
        new()
        {
            {
                new(
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: TimesheetProjectType.Project,
                        displayName: "Some lead display name"),
                    duration: 8,
                    description: "Some message!",
                    timesheetId: Guid.Parse("4e5b095b-ef99-4623-b493-60adc8992a9c")),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(Guid.Parse("4e5b095b-ef99-4623-b493-60adc8992a9c")),
                    entityData: new Dictionary<string, object?>
                    {
                        ["gg_description"] = "Some message!",
                        ["gg_duration"] = 8,
                        ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(7583b4e6-23f5-eb11-94ef-00224884a588)",
                        ["subject"] = "Some lead display name"
                    })
            }
        };
}