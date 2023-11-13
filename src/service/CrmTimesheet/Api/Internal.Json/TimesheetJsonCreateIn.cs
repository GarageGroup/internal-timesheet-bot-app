using System;
using System.Collections.Generic;
using System.Globalization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJsonCreateIn
{
    public required TimesheetProjectType ProjectType { get; init; }

    public required Guid ProjectId { get; init; }

    public required DateOnly Date { get; init; }

    public required string? Description { get; init; }

    public required decimal Duration { get; init; }

    public required int? ChannelCode { get; init; }

    internal IReadOnlyDictionary<string, object?> BuildEntity()
    {
        (string name, string pluralName) = ProjectType switch
        {
            IncidentJson.Type       => (IncidentJson.EntityName, IncidentJson.EntitySetName),
            LeadJson.Type           => (LeadJson.EntityName, LeadJson.EntitySetName),
            OpportunityJson.Type    => (OpportunityJson.EntityName, OpportunityJson.EntitySetName),
            ProjectJson.Type        => (ProjectJson.EntityName, ProjectJson.EntitySetName),
            _ => throw new InvalidOperationException($"An unexpected project type: {ProjectType}")
        };

        var entityData = new Dictionary<string, object?>
        {
            [$"regardingobjectid_{name}@odata.bind"] = $"/{pluralName}({ProjectId:D})",
            ["gg_date"] = Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["gg_description"] = Description,
            ["gg_duration"] = Duration
        };

        if (ChannelCode is not null)
        {
            entityData["gg_timesheetactivity_channel"] = ChannelCode;
        }

        return entityData;
    }
}