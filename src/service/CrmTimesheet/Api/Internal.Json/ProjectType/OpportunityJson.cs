using System;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class OpportunityJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(NameFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Opportunity;

    public const string EntityName = "opportunity";

    public const string EntitySetName = "opportunities";

    private const string IdFieldName = "opportunityid";

    private const string NameFieldName = "name";

    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }

    [JsonPropertyName(NameFieldName)]
    public string? Name { get; init; }

    TimesheetProjectType ITimesheetProjectType.Type
        =>
        Type;
}