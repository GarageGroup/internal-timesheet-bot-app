using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmProject.Test;

partial class CrmProjectApiSource
{
    public static IEnumerable<object[]> InputGetLastTestData
        =>
        [
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("bef33be0-99f5-4018-ba80-3366ec9ec1fd"),
                    top: 7),
                new CrmProjectApiOption
                {
                    LastProjectItemsCount = null,
                    LastProjectDaysCount = 5
                },
                new DateOnly(2022, 02, 01),
                new DataverseEntitySetGetIn(
                    entityPluralName: "gg_timesheetactivities",
                    selectFields: new("gg_date"),
                    expandFields: new(
                        new("regardingobjectid_incident", new("title")),
                        new("regardingobjectid_lead", new("subject", "companyname")),
                        new("regardingobjectid_opportunity", new("name")),
                        new("regardingobjectid_gg_project", new("gg_name"))),
                    filter:
                        "(_ownerid_value eq 'bef33be0-99f5-4018-ba80-3366ec9ec1fd' " +
                        "and _regardingobjectid_value ne null and gg_date lt 2022-02-02 and gg_date gt 2022-01-27)",
                    orderBy: new(
                        new("gg_date", DataverseOrderDirection.Descending),
                        new("createdon", DataverseOrderDirection.Descending)),
                    top: null)
            ],
            [
                new LastProjectSetGetIn(
                    userId: Guid.Parse("e0ede566-276c-4d56-b8d7-aed2f411463e"),
                    top: 3),
                new CrmProjectApiOption
                {
                    LastProjectItemsCount = 71,
                    LastProjectDaysCount = null
                },
                new DateOnly(2022, 01, 17),
                new DataverseEntitySetGetIn(
                    entityPluralName: "gg_timesheetactivities",
                    selectFields: new("gg_date"),
                    expandFields: new(
                        new("regardingobjectid_incident", new("title")),
                        new("regardingobjectid_lead", new("subject", "companyname")),
                        new("regardingobjectid_opportunity", new("name")),
                        new("regardingobjectid_gg_project", new("gg_name"))),
                    filter:
                        "(_ownerid_value eq 'e0ede566-276c-4d56-b8d7-aed2f411463e' " +
                        "and _regardingobjectid_value ne null and gg_date lt 2022-01-18)",
                    orderBy: new(
                        new("gg_date", DataverseOrderDirection.Descending),
                        new("createdon", DataverseOrderDirection.Descending)),
                    top: 71)
            ]
        ];
}