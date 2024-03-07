using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class UpdateTimesheetJson
{
    public required Guid? ProjectId { get; init; }

    public required TimesheetProjectType? ProjectType { get; init; }

    public required string? ProjectDisplayName { get; init; }

    public required decimal? Duration { get; init; }

    public required string? Description { get; init; }

    public Guid Id { get; init; }

    internal Result<IReadOnlyDictionary<string, object?>, Failure<Unit>> BuildEntityOrFailure()
    {
        var entityData = new Dictionary<string, object?>();

        if (Duration != null)
        {
            entityData["gg_duration"] = Duration; 
        }

        if (Description != null)
        {
            entityData["gg_description"] = Description;
        }

        if (ProjectId != null && ProjectType != null && ProjectDisplayName != null)
        {
            _ = ProjectType switch
            {
                TimesheetProjectType.Incident => entityData["regardingobjectid_incident@odata.bind"] = $"/incidents({ProjectId:D})",
                TimesheetProjectType.Lead => entityData["regardingobjectid_lead@odata.bind"] = $"/leads({ProjectId:D})",
                TimesheetProjectType.Opportunity => entityData["regardingobjectid_opportunity@odata.bind"] = $"/opportunities({ProjectId:D})",
                TimesheetProjectType.Project => entityData["regardingobjectid_gg_project@odata.bind"] = $"/gg_projects({ProjectId:D})",
                _ => null
            };

            if (string.IsNullOrEmpty(ProjectDisplayName) is false)
            {
                entityData["subject"] = ProjectDisplayName;
            }
        }

        if (entityData.Count == 0)
        {
            return Failure.Create("Нет ни одного поля для изменения");
        }

        return entityData;
    }
}
