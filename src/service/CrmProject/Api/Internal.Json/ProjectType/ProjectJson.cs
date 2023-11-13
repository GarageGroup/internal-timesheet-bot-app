using System;
using System.Linq;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class ProjectJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(NameFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Project;

    public const string EntityName = "gg_project";

    public const string EntitySetName = "gg_projects";

    private const string IdFieldName = "gg_projectid";

    private const string NameFieldName = "gg_name";

    internal static ProjectJson? FromSearchItem(DataverseSearchItem item)
    {
        if (string.Equals(item.EntityName, EntityName, StringComparison.InvariantCulture) is false)
        {
            return null;
        }

        return new()
        {
            Id = item.ObjectId,
            Name = item.ExtensionData.AsEnumerable().GetValueOrAbsent(NameFieldName).OrDefault()?.ToString()
        };
    }

    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }

    [JsonPropertyName(NameFieldName)]
    public string? Name { get; init; }

    TimesheetProjectType ITimesheetProjectType.Type
        =>
        Type;
}