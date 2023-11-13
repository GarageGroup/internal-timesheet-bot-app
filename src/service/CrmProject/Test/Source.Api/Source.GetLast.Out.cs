using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static IEnumerable<object[]> OutputGetLastTestData
        =>
        [
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("34c54a6c-f660-47d4-8847-539672df2258"),
                    top: 5),
                default(DataverseEntitySetGetOut<LastTimesheetItemJson>),
                default(LastProjectSetGetOut)
            ],
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("e6414947-9035-4123-94b1-dca091c7c781"),
                    top: -1),
                new DataverseEntitySetGetOut<LastTimesheetItemJson>(
                    value: new LastTimesheetItemJson[]
                    {
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("3ccee31a-63be-45ad-9697-853a8746d38d"),
                                Name = "Some project name"
                            },
                            TimesheetDate = new(2021, 07, 21, 15, 25, 01)
                        }
                    }),
                default(LastProjectSetGetOut)
            ],
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("7864fb8e-f663-4521-86db-96d1f824b196"),
                    top: 5),
                new DataverseEntitySetGetOut<LastTimesheetItemJson>(
                    value: new LastTimesheetItemJson[]
                    {
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"),
                                Name = "First project name"
                            },
                            TimesheetDate = new(2021, 07, 21, 15, 25, 01)
                        },
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"),
                                Name = "Project number 2"
                            },
                            TimesheetDate = new(2022, 01, 11, 05, 27, 17)
                        },
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"),
                                Name = "Some project name"
                            },
                            TimesheetDate = new(2021, 12, 01)
                        },
                        new()
                        {
                            Lead = new()
                            {
                                Id = Guid.Parse("03a107b9-6a35-4306-b61f-995e93b26e44"),
                                Subject = "Some fourth name"
                            },
                            TimesheetDate = new(2021, 05, 23),
                        },
                        new()
                        {
                            Opportunity = new()
                            {
                                Id = Guid.Parse("b55d6889-308a-47e9-b3d7-c7e3d3af2f53"),
                                Name = "Fifth project"
                            },
                            TimesheetDate = new(2020, 11, 03),
                        },
                        new()
                        {
                            TimesheetDate = new(2022, 01, 15),
                        },
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"),
                                Name = "Project 7"
                            },
                            TimesheetDate = new(2022, 01, 11, 05, 27, 17)
                        },
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("7d54bf8d-add9-4414-a3ab-80e56eea6807"),
                                Name = null
                            },
                            TimesheetDate = new(2021, 01, 31, 11, 25, 17),
                        },
                        new()
                        {
                            Incident = new()
                            {
                                Id = Guid.Parse("a29e3a95-8ddd-48df-a605-3a4d6a15567f"),
                                Title = "Some eighth project name"
                            },
                            TimesheetDate = new(2022, 02, 03),
                        },
                        new()
                        {
                            Project = new()
                            {
                                Id = Guid.Parse("35436d82-7a15-425e-a782-15b23141d43a"),
                                Name = "The ninth project"
                            },
                            TimesheetDate = new(2022, 01, 15),
                        }
                    }),
                new LastProjectSetGetOut
                {
                    Projects = new ProjectSetGetItem[]
                    {
                        new(Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"), "First project name", TimesheetProjectType.Project),
                        new(Guid.Parse("03a107b9-6a35-4306-b61f-995e93b26e44"), "Some fourth name", TimesheetProjectType.Lead),
                        new(Guid.Parse("b55d6889-308a-47e9-b3d7-c7e3d3af2f53"), "Fifth project", TimesheetProjectType.Opportunity),
                        new(Guid.Parse("7d54bf8d-add9-4414-a3ab-80e56eea6807"), string.Empty, TimesheetProjectType.Project),
                        new(Guid.Parse("a29e3a95-8ddd-48df-a605-3a4d6a15567f"), "Some eighth project name", TimesheetProjectType.Incident)
                    }
                }
            ]
        ];
}