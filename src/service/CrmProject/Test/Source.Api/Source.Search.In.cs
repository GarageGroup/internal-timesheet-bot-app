using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static IEnumerable<object[]> InputSearchTestData
        =>
        [
            [
                new ProjectSetSearchIn(
                    searchText: null,
                    userId: Guid.Parse("4812cb4b-3049-4f84-bfb6-aac8f0425028"),
                    top: 3),
                new DataverseSearchIn("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = 3
                }
            ],
            [
                new ProjectSetSearchIn(
                    searchText: string.Empty,
                    userId: Guid.Parse("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    top: -2),
                new DataverseSearchIn("**")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = -2
                }
            ],
            [
                new ProjectSetSearchIn(
                    searchText: "Some text",
                    userId: Guid.Parse("32b49b76-01ca-4312-b7d5-499cf3addc22"),
                    top: 15),
                new DataverseSearchIn("*Some text*")
                {
                    Entities = new("gg_project", "lead", "opportunity", "incident"),
                    Top = 15
                }
            ]
        ];
}