using System;
using System.Collections.Generic;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    public const string EntityPluralName = "gg_timesheetactivities";

    public required TimesheetProjectType ProjectType { get; init; }

    public required Guid ProjectId { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Description { get; init; }

    public required decimal Duration { get; init; }

    public required int? ChannelCode { get; init; }

    internal IReadOnlyDictionary<string, object?> BuildEntity()
    {
        var entityData = new Dictionary<string, object?>
        {
            ["gg_date"] = Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["gg_description"] = Description,
            ["gg_duration"] = Duration
        };

        if (ProjectType is TimesheetProjectType.Incident)
        {
            entityData["regardingobjectid_incident@odata.bind"] = $"/incidents({ProjectId:D})";
        }
        else if (ProjectType is TimesheetProjectType.Lead)
        {
            entityData["regardingobjectid_lead@odata.bind"] = $"/leads({ProjectId:D})";
        }
        else if (ProjectType is TimesheetProjectType.Opportunity)
        {
            entityData["regardingobjectid_opportunity@odata.bind"] = $"/opportunities({ProjectId:D})";
        }
        else if (ProjectType is TimesheetProjectType.Project)
        {
            entityData["regardingobjectid_gg_project@odata.bind"] = $"/gg_projects({ProjectId:D})";
        }
        else
        {
            throw new InvalidOperationException($"An unexpected project type: {ProjectType}");
        }

        if (ChannelCode is not null)
        {
            entityData["gg_timesheetactivity_channel"] = ChannelCode;
        }

        return entityData;
    }
}