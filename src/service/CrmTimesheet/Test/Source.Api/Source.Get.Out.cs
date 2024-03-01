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
                        ProjectName = "SomeFirstProjectName",
                        Subject = null,
                        Description = "Some description",
                        Id = Guid.Parse("c19387fa-7bbd-45ae-bc5f-8b2003c764af"),
                    },
                    new()
                    {
                        Duration = 5,
                        ProjectName = "Some Lead",
                        Subject = "Some Lead Name",
                        Description = null,
                        Id = Guid.Parse("64aa110f-258a-4771-8a6b-21e8fb9fed5d"),
                    },
                    new()
                    {
                        Duration = 2.5m,
                        ProjectName = "Some Second Lead",
                        Subject = "\n\r",
                        Description = string.Empty,
                        Id = Guid.Parse("a4db6d8e-a632-4f9c-ad8c-1c49261b6d85"),
                    },
                    new()
                    {
                        Duration = 7,
                        ProjectName = "Third company",
                        Subject = string.Empty,
                        Description = "Some lead description",
                        Id = Guid.Parse("6f565e16-024a-4012-ad9d-150e32216125"),
                    },
                    new()
                    {
                        Duration = 0,
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
                            projectName : "SomeFirstProjectName",
                            description : "Some description",
                            id: Guid.Parse("c19387fa-7bbd-45ae-bc5f-8b2003c764af")),
                        new(
                            duration : 5,
                            projectName : "Some Lead Name",
                            description : string.Empty,
                            id: Guid.Parse("64aa110f-258a-4771-8a6b-21e8fb9fed5d")),
                        new(
                            duration : 2.5m,
                            projectName : "\n\r",
                            description : string.Empty,
                            id: Guid.Parse("a4db6d8e-a632-4f9c-ad8c-1c49261b6d85")),
                        new(
                            duration : 7,
                            projectName : "Third company",
                            description : "Some lead description",
                            id: Guid.Parse("6f565e16-024a-4012-ad9d-150e32216125")),
                        new(
                            duration : 0,
                            projectName : null,
                            description : string.Empty,
                            id: Guid.Parse("36d5fda9-fa30-45cb-bbb5-53df2d4e4d72"))
                    ]
                }
            }
        };
}