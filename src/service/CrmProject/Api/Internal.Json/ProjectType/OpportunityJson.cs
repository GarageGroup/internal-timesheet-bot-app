using System;
using System.Linq;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class OpportunityJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(NameFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Opportunity;

    public const string EntityName = "opportunity";

    public const string EntitySetName = "opportunities";

    private const string IdFieldName = "opportunityid";

    private const string NameFieldName = "name";

    internal static OpportunityJson? FromSearchItem(DataverseSearchItem item)
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