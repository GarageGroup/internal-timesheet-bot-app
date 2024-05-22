using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson
{
    public Guid Id { get; init; }

    public TimesheetProjectType Type { get; init; }

    public string? Name { get; init; }

    [JsonPropertyName("typeName")]
    public string? TypeDisplayName { get; init; }
}