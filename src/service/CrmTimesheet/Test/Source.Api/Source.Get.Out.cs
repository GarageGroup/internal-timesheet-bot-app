using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static IEnumerable<object[]> OutputGetTestData
        =>
        [
            [
                default(DataverseEntitySetGetOut<TimesheetItemJson>),
                default(TimesheetSetGetOut)
            ],
            [
                new DataverseEntitySetGetOut<TimesheetItemJson>(
                    value: new TimesheetItemJson[]
                    {
                        new()
                        {
                            TimesheetId = Guid.Parse("2f65e056-01d5-ec11-a7b5-0022487fa37b"),
                            Date = new(2022, 02, 15, 08, 05, 56, TimeSpan.FromHours(1)),
                            Duration = 1,
                            Description = "Some description",
                            Project = new()
                            {
                                Name = "SomeFirstProjectName"
                            }
                        },
                        new()
                        {
                            TimesheetId = Guid.Parse("a5768c20-04d5-ec11-a7b5-0022487fa37b"),
                            Date = new(2022, 03, 05, 10, 51, 24, TimeSpan.FromHours(3)),
                            Duration = 5,
                            Description = null,
                            Lead = new()
                            {
                                Subject = "Some Lead",
                                CompanyName = "First Company"
                            }
                        },
                        new()
                        {
                            TimesheetId = Guid.Parse("ab3b5ae8-e0ae-47b6-93c7-94a28486135b"),
                            Date = new(2023, 01, 17, default, default, default, default),
                            Duration = 2.5m,
                            Description = null,
                            Lead = new()
                            {
                                Subject = "Some Second Lead",
                                CompanyName = string.Empty
                            }
                        },
                        new()
                        {
                            TimesheetId = Guid.Parse("2ca1400a-0ae9-494b-b96c-315cdc3b0d59"),
                            Date = new(2023, 02, 21, default, default, default, default),
                            Duration = 7,
                            Description = "Some lead description",
                            Lead = new()
                            {
                                Subject = string.Empty,
                                CompanyName = "Third company"
                            }
                        },
                        new()
                        {
                            TimesheetId = Guid.Parse("89ca0e70-9fba-44ad-8e55-36e8eb223d3f"),
                            Date = new(2023, 01, 15, default, default, default, default),
                            Duration = 0,
                            Description = string.Empty
                        }
                    }),
                new TimesheetSetGetOut
                {
                    Timesheets = new TimesheetSetGetItem[]
                    {
                        new(
                            timesheetId : Guid.Parse("2f65e056-01d5-ec11-a7b5-0022487fa37b"),
                            date : new(2022, 02, 15),
                            duration : 1,
                            projectName : "SomeFirstProjectName",
                            description : "Some description"),
                        new(
                            timesheetId : Guid.Parse("a5768c20-04d5-ec11-a7b5-0022487fa37b"),
                            date : new(2022, 03, 05),
                            duration : 5,
                            projectName : "Some Lead (First Company)",
                            description : null),
                        new(
                            timesheetId : Guid.Parse("ab3b5ae8-e0ae-47b6-93c7-94a28486135b"),
                            date : new(2023, 01, 17),
                            duration : 2.5m,
                            projectName : "Some Second Lead",
                            description : null),
                        new(
                            timesheetId : Guid.Parse("2ca1400a-0ae9-494b-b96c-315cdc3b0d59"),
                            date : new(2023, 02, 21),
                            duration : 7,
                            projectName : "(Third company)",
                            description : "Some lead description"),
                        new(
                            timesheetId : Guid.Parse("89ca0e70-9fba-44ad-8e55-36e8eb223d3f"),
                            date : new(2023, 01, 15),
                            duration : 0,
                            projectName : null,
                            description : null)
                    }
                }
            ]
        ];
}