using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(NameFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Project;

    public const string EntityName = "gg_project";

    public const string EntitySetName = "gg_projects";

    private const string IdFieldName = "gg_projectid";

    private const string NameFieldName = "gg_name";

    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }

    [JsonPropertyName(NameFieldName)]
    public string? Name { get; init; }

    TimesheetProjectType ITimesheetProjectType.Type
        =>
        Type;
}