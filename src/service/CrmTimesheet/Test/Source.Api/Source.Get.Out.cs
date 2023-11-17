using System;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static IEnumerable<object[]> OutputGetTestData
        =>
        [
            [
                default(FlatArray<DbTimesheet>),
                default(TimesheetSetGetOut)
            ],
            [
                new DbTimesheet[]
                {
                    new()
                    {
                        Duration = 1,
                        ProjectName = "SomeFirstProjectName",
                        Subject = null,
                        Description = "Some description"
                    },
                    new()
                    {
                        Duration = 5,
                        ProjectName = "Some Lead",
                        Subject = "Some Lead Name",
                        Description = null
                    },
                    new()
                    {
                        Duration = 2.5m,
                        ProjectName = "Some Second Lead",
                        Subject = "\n\r",
                        Description = string.Empty
                    },
                    new()
                    {
                        Duration = 7,
                        ProjectName = "Third company",
                        Subject = string.Empty,
                        Description = "Some lead description"
                    },
                    new()
                    {
                        Duration = 0,
                        Description = string.Empty
                    }
                },
                new TimesheetSetGetOut
                {
                    Timesheets = new TimesheetSetGetItem[]
                    {
                        new(
                            duration : 1,
                            projectName : "SomeFirstProjectName",
                            description : "Some description"),
                        new(
                            duration : 5,
                            projectName : "Some Lead Name",
                            description : string.Empty),
                        new(
                            duration : 2.5m,
                            projectName : "\n\r",
                            description : string.Empty),
                        new(
                            duration : 7,
                            projectName : "Third company",
                            description : "Some lead description"),
                        new(
                            duration : 0,
                            projectName : null,
                            description : string.Empty)
                    }
                }
            ]
        ];
}