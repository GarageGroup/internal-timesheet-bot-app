using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class IncidentJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(TitleFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Incident;

    public const string EntityName = "incident";

    public const string EntitySetName = "incidents";

    private const string IdFieldName = "incidentid";

    private const string TitleFieldName = "title";

    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }

    [JsonPropertyName(TitleFieldName)]
    public string? Title { get; init; }

    string? ITimesheetProjectType.Name
        =>
        Title;

    TimesheetProjectType ITimesheetProjectType.Type
        =>
        Type;
}