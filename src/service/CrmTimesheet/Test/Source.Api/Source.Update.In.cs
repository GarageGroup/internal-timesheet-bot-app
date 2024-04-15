using GarageGroup.Infra;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static TheoryData<TimesheetUpdateIn, DataverseEntityUpdateIn<TimesheetJson>> InputUpdateTestData
        =>
        new()
        {
            {
                new(
                    timesheetId: new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635"),
                    date: new(2021, 10, 07),
                    project: new(
                        id: new("7583b4e6-23f5-eb11-94ef-00224884a588"),
                        type: TimesheetProjectType.Lead,
                        displayName: "Some lead display name"),
                    duration: 8,
                    description: "Some message!"),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("555685cd-bdfb-42a5-aee6-8ad7f9c3b635")),
                    entityData: new()
                    {
                        Date = new(2021, 10, 07),
                        Description = "Some message!",
                        Duration = 8,
                        LeadLookupValue = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)",
                        Subject = "Some lead display name"
                    })
            },
            {
                new(
                    timesheetId: new("ee725221-ef2c-4517-8cf3-b4300b99f634"),
                    date: new(2023, 01, 12),
                    project: new(
                        id: new("8829deda-5249-4412-9be5-ef5728fb928d"),
                        type: TimesheetProjectType.Opportunity,
                        displayName: string.Empty),
                    duration: 3,
                    description: null),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("ee725221-ef2c-4517-8cf3-b4300b99f634")),
                    entityData: new()
                    {
                        Date = new(2023, 01, 12),
                        Description = null,
                        Duration = 3,
                        OpportunityLookupValue = "/opportunities(8829deda-5249-4412-9be5-ef5728fb928d)"
                    })
            },
            {
                new(
                    timesheetId: new("af97dc90-cbab-48f0-a5ec-2a99d73a4c4f"),
                    date: new(2023, 11, 03),
                    project: new(
                        id: new("13f0cb5c-b251-494c-9cae-1b0708471c10"),
                        type: TimesheetProjectType.Project,
                        displayName: "\n\r"),
                    duration: 15,
                    description: string.Empty),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("af97dc90-cbab-48f0-a5ec-2a99d73a4c4f")),
                    entityData: new()
                    {
                        Date = new(2023, 11, 03),
                        Description = null,
                        Duration = 15,
                        ProjectLookupValue = "/gg_projects(13f0cb5c-b251-494c-9cae-1b0708471c10)",
                        Subject = "\n\r"
                    })
            },
            {
                new(
                    timesheetId: new("f49ab8d9-0890-4cbc-8b24-75b65acd4780"),
                    date: new(2022, 12, 25),
                    project: new(
                        id: new("ca012870-a0f9-4945-a314-a14ebf690574"),
                        type: TimesheetProjectType.Incident,
                        displayName: null),
                    duration: -3,
                    description: "Some description"),
                new(
                    entityPluralName: "gg_timesheetactivities",
                    entityKey: new DataversePrimaryKey(new("f49ab8d9-0890-4cbc-8b24-75b65acd4780")),
                    entityData: new()
                    {
                        Date = new(2022, 12, 25),
                        Description = "Some description",
                        Duration = -3,
                        IncidentLookupValue = "/incidents(ca012870-a0f9-4945-a314-a14ebf690574)"
                    })
            }
        };
}