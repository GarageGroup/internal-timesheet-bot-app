﻿using System;
using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static IEnumerable<object[]> OutputGetLastTestData
        =>
        [
            [
                default(FlatArray<DbTimesheetProject>),
                default(LastProjectSetGetOut)
            ],
            [
                new DbTimesheetProject[]
                {
                    new()
                    {
                        ProjectId = Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"),
                        ProjectName = "Some project name",
                        ProjectTypeCode = 10912,
                        Subject = null
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("d3742670-803c-4c12-92df-ebb5cbed5670"),
                        ProjectName = "Some another text",
                        ProjectTypeCode = 4,
                        Subject = "Some lead name",
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("a88a510a-1633-49e1-b278-c502fa4fe5c0"),
                        ProjectName = "Some name",
                        ProjectTypeCode = 5,
                        Subject = "Some subject",
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("b55d6889-308a-47e9-b3d7-c7e3d3af2f53"),
                        ProjectName = "Some Opportunity Name",
                        ProjectTypeCode = 3,
                        Subject = string.Empty,
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("6786f494-caef-41f9-9ce9-7f75221b4d0f"),
                        ProjectName = null,
                        ProjectTypeCode = 3,
                        Subject = null,
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("7d54bf8d-add9-4414-a3ab-80e56eea6807"),
                        ProjectName = "Second Project",
                        ProjectTypeCode = 10912,
                        Subject = "\n\t",
                    },
                    new()
                    {
                        ProjectId = Guid.Parse("f1d8d51b-cdb4-4d00-ac16-46b65e036d9f"),
                        ProjectName = "Second incident name",
                        ProjectTypeCode = 112,
                        Subject = string.Empty,
                    }
                },
                new LastProjectSetGetOut
                {
                    Projects = new ProjectSetGetItem[]
                    {
                        new(Guid.Parse("9bc7aebd-33f2-4caf-966d-3073b3554ca3"), "Some project name", TimesheetProjectType.Project),
                        new(Guid.Parse("d3742670-803c-4c12-92df-ebb5cbed5670"), "Some lead name", TimesheetProjectType.Lead),
                        new(Guid.Parse("a88a510a-1633-49e1-b278-c502fa4fe5c0"), "Some subject", (TimesheetProjectType)5),
                        new(Guid.Parse("b55d6889-308a-47e9-b3d7-c7e3d3af2f53"), "Some Opportunity Name", TimesheetProjectType.Opportunity),
                        new(Guid.Parse("6786f494-caef-41f9-9ce9-7f75221b4d0f"), string.Empty, TimesheetProjectType.Opportunity),
                        new(Guid.Parse("7d54bf8d-add9-4414-a3ab-80e56eea6807"), "\n\t", TimesheetProjectType.Project),
                        new(Guid.Parse("f1d8d51b-cdb4-4d00-ac16-46b65e036d9f"), "Second incident name", TimesheetProjectType.Incident)
                    }
                }
            ]
        ];
}