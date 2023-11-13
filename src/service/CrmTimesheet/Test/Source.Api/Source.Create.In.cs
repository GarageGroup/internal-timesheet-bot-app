using System;
using System.Collections.Generic;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet.Service.CrmTimesheet.Test;

partial class CrmTimesheetApiSource
{
    public static IEnumerable<object[]> InputCreateTestData
        =>
        [
            [
                new TimesheetCreateIn(
                    userId: Guid.Parse("ded7a0d5-33c8-4e02-affe-61559ef4d4ca"),
                    date: new(2021, 10, 07),
                    projectId: Guid.Parse("7583b4e6-23f5-eb11-94ef-00224884a588"),
                    projectType: TimesheetProjectType.Lead,
                    duration: 8,
                    description: "Some message!",
                    channel: TimesheetChannel.Telegram),
                default(CrmTimesheetApiOption),
                new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new Dictionary<string, object?>
                    {
                        ["gg_date"] = "2021-10-07",
                        ["gg_description"] = "Some message!",
                        ["gg_duration"] = 8,
                        ["regardingobjectid_lead@odata.bind"] = "/leads(7583b4e6-23f5-eb11-94ef-00224884a588)"
                    })
            ],
            [
                new TimesheetCreateIn(
                    userId: Guid.Parse("cede85e3-d0db-44d3-8728-ce42549eb4d0"),
                    date: new(2023, 01, 12),
                    projectId: Guid.Parse("8829deda-5249-4412-9be5-ef5728fb928d"),
                    projectType: TimesheetProjectType.Opportunity,
                    duration: 3,
                    description: null,
                    channel: TimesheetChannel.Teams),
                new CrmTimesheetApiOption(
                    channelCodes: new KeyValuePair<TimesheetChannel, int?>[]
                    {
                        new(TimesheetChannel.WebChat, 271),
                        new(TimesheetChannel.Teams, 279015),
                        new(TimesheetChannel.Teams, 123124)
                    }),
                new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new Dictionary<string, object?>
                    {
                        ["gg_date"] = "2023-01-12",
                        ["gg_description"] = null,
                        ["gg_duration"] = 3,
                        ["regardingobjectid_opportunity@odata.bind"] = "/opportunities(8829deda-5249-4412-9be5-ef5728fb928d)",
                        ["gg_timesheetactivity_channel"] = 279015
                    })
            ],
            [
                new TimesheetCreateIn(
                    userId: Guid.Parse("ce3e2f48-8eec-40f0-bb8b-60b8861a61cd"),
                    date: new(2023, 11, 03),
                    projectId: Guid.Parse("13f0cb5c-b251-494c-9cae-1b0708471c10"),
                    projectType: TimesheetProjectType.Project,
                    duration: 15,
                    description: string.Empty,
                    channel: TimesheetChannel.Telegram),
                new CrmTimesheetApiOption(
                    channelCodes: new KeyValuePair<TimesheetChannel, int?>[]
                    {
                        new(TimesheetChannel.Telegram, null)
                    }),
                new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new Dictionary<string, object?>
                    {
                        ["gg_date"] = "2023-11-03",
                        ["gg_description"] = null,
                        ["gg_duration"] = 15,
                        ["regardingobjectid_gg_project@odata.bind"] = "/gg_projects(13f0cb5c-b251-494c-9cae-1b0708471c10)"
                    })
            ],
            [
                new TimesheetCreateIn(
                    userId: Guid.Parse("c59436f5-709c-45aa-8469-5e79412f5108"),
                    date: new(2022, 12, 25),
                    projectId: Guid.Parse("ca012870-a0f9-4945-a314-a14ebf690574"),
                    projectType: TimesheetProjectType.Incident,
                    duration: -3,
                    description: "Some description",
                    channel: TimesheetChannel.Telegram),
                new CrmTimesheetApiOption(
                    channelCodes: new KeyValuePair<TimesheetChannel, int?>[]
                    {
                        new(TimesheetChannel.Telegram, -967912307),
                        new(TimesheetChannel.WebChat, 12000),
                        new(TimesheetChannel.Teams, 112497)
                    }),
                new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                    entityPluralName: "gg_timesheetactivities",
                    entityData: new Dictionary<string, object?>
                    {
                        ["gg_date"] = "2022-12-25",
                        ["gg_description"] = "Some description",
                        ["gg_duration"] = -3,
                        ["regardingobjectid_incident@odata.bind"] = "/incidents(ca012870-a0f9-4945-a314-a14ebf690574)",
                        ["gg_timesheetactivity_channel"] = -967912307
                    })
            ]
        ];
}