using System;
using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static TheoryData<ProjectSetSearchIn, DataverseSearchIn> InputSearchTestData
        =>
        new()
        {
            {
                new(
                    searchText: null,
                    userId: Guid.Parse("4812cb4b-3049-4f84-bfb6-aac8f0425028"),
                    top: 3),
                new("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = 3
                }
            },
            {
                new(
                    searchText: string.Empty,
                    userId: Guid.Parse("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    top: -2),
                new("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = -2
                }
            },
            {
                new(
                    searchText: "Some text",
                    userId: Guid.Parse("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    top: 15),
                new("*Some text*")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = 15
                }
            }
        };
}