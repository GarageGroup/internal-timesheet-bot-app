using System;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static TheoryData<FlatArray<DbTimesheet>, TimesheetSetGetOut> OutputGetTestData
        =>
        new()
        {
            {
                default,
                default
            },
            {
                [
                    new()
                    {
                        Duration = 1,
                        ProjectId = Guid.Parse("148e2a2e-2485-468d-9d58-90f348fd2398"),
                        ProjectTypeCode = 112,
                        ProjectName = "SomeFirstProjectName",
                        Subject = null,
                        Description = "Some description",
                        Id = Guid.Parse("c19387fa-7bbd-45ae-bc5f-8b2003c764af"),
                    },
                    new()
                    {
                        Duration = 5,
                        ProjectId = Guid.Parse("6402b74b-ab14-4332-86f1-ceeac380f7d7"),
                        ProjectTypeCode = 3,
                        ProjectName = "Some Lead",
                        Subject = "Some Lead Name",
                        Description = null,
                        Id = Guid.Parse("64aa110f-258a-4771-8a6b-21e8fb9fed5d"),
                    },
                    new()
                    {
                        Duration = 2.5m,
                        ProjectId = Guid.Parse("52eb8367-f30f-4571-a6b5-333d59fbedf0"),
                        ProjectTypeCode = 10912,
                        ProjectName = "Some Second Lead",
                        Subject = "\n\r",
                        Description = string.Empty,
                        Id = Guid.Parse("a4db6d8e-a632-4f9c-ad8c-1c49261b6d85"),
                    },
                    new()
                    {
                        Duration = 7,
                        ProjectId = Guid.Parse("de1a7bf8-7991-4c2a-870d-e6153e83ee0a"),
                        ProjectTypeCode = -15,
                        ProjectName = "Third company",
                        Subject = string.Empty,
                        Description = "Some lead description",
                        Id = Guid.Parse("6f565e16-024a-4012-ad9d-150e32216125"),
                    },
                    new()
                    {
                        Duration = 0,
                        ProjectId = Guid.Parse("db08f90b-2845-4092-ab38-05af92c8a433"),
                        ProjectTypeCode = 4,
                        Description = string.Empty,
                        Id = Guid.Parse("36d5fda9-fa30-45cb-bbb5-53df2d4e4d72"),
                    }
                ],
                new()
                {
                    Timesheets =
                    [
                        new(
                            duration : 1,
                            projectId: Guid.Parse("148e2a2e-2485-468d-9d58-90f348fd2398"),
                            projectType: TimesheetProjectType.Incident,
                            projectName : "SomeFirstProjectName",
                            description : "Some description",
                            id: Guid.Parse("c19387fa-7bbd-45ae-bc5f-8b2003c764af")),
                        new(
                            duration : 5,
                            projectId: Guid.Parse("6402b74b-ab14-4332-86f1-ceeac380f7d7"),
                            projectType: TimesheetProjectType.Opportunity,
                            projectName : "Some Lead Name",
                            description : string.Empty,
                            id: Guid.Parse("64aa110f-258a-4771-8a6b-21e8fb9fed5d")),
                        new(
                            duration : 2.5m,
                            projectId: Guid.Parse("52eb8367-f30f-4571-a6b5-333d59fbedf0"),
                            projectType: TimesheetProjectType.Project,
                            projectName : "\n\r",
                            description : string.Empty,
                            id: Guid.Parse("a4db6d8e-a632-4f9c-ad8c-1c49261b6d85")),
                        new(
                            duration : 7,
                            projectId: Guid.Parse("de1a7bf8-7991-4c2a-870d-e6153e83ee0a"),
                            projectType: (TimesheetProjectType)(-15),
                            projectName : "Third company",
                            description : "Some lead description",
                            id: Guid.Parse("6f565e16-024a-4012-ad9d-150e32216125")),
                        new(
                            duration : 0,
                            projectId: Guid.Parse("db08f90b-2845-4092-ab38-05af92c8a433"),
                            projectType: TimesheetProjectType.Lead,
                            projectName : null,
                            description : string.Empty,
                            id: Guid.Parse("36d5fda9-fa30-45cb-bbb5-53df2d4e4d72"))
                    ]
                }
            }
        };
}