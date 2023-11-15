using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetItemJson
{
    public const string EntityPluralName = "gg_timesheetactivities";

    public static readonly FlatArray<string> SelectedFields
        =
        new(DateFieldName, DurationFieldName, DescriptionFieldName);

    public static readonly FlatArray<DataverseExpandedField> ExpandedFields
        =
        new(
            new(IncidentFieldName, IncidentJson.FieldNames),
            new(LeadFieldName, LeadJson.FieldNames),
            new(OpportunityFieldName, OpportunityJson.FieldNames),
            new(ProjectFieldName, ProjectJson.FieldNames));

    public static readonly FlatArray<DataverseOrderParameter> OrderFields
        =
        new DataverseOrderParameter("createdon", DataverseOrderDirection.Ascending).AsFlatArray();

    private const string IncidentFieldName = "regardingobjectid_incident";

    private const string LeadFieldName = "regardingobjectid_lead";

    private const string OpportunityFieldName = "regardingobjectid_opportunity";

    private const string ProjectFieldName = "regardingobjectid_gg_project";

    private const string DateFieldName = "gg_date";

    private const string DurationFieldName = "gg_duration";

    private const string DescriptionFieldName = "gg_description";

    [JsonPropertyName("regardingobjectid_incident")]
    public IncidentJson? Incident { get; init; }

    [JsonPropertyName("regardingobjectid_lead")]
    public LeadJson? Lead { get; init; }

    [JsonPropertyName("regardingobjectid_opportunity")]
    public OpportunityJson? Opportunity { get; init; }

    [JsonPropertyName("regardingobjectid_gg_project")]
    public ProjectJson? Project { get; init; }

    [JsonPropertyName("activityid")]
    public Guid TimesheetId { get; init; }

    [JsonPropertyName(DateFieldName)]
    public DateTimeOffset Date { get; init; }

    [JsonPropertyName(DurationFieldName)]
    public decimal Duration { get; init; }

    [JsonPropertyName(DescriptionFieldName)]
    public string? Description { get; init; }

    public ITimesheetProjectType? GetProjectType()
    {
        if (Incident is not null)
        {
            return Incident;
        }

        if (Lead is not null)
        {
            return Lead;
        }

        if (Opportunity is not null)
        {
            return Opportunity;
        }

        if (Project is not null)
        {
            return Project;
        }

        return null;
    }

    internal static string BuildFilter(Guid userId, DateOnly date)
    {
        var dateValue = DataverseFilterValue.FromRawString(date.ToString("yyyy-MM-dd"));

        return new DataverseLogicalFilter(DataverseLogicalOperator.And)
        {
            Filters = new(
                new DataverseComparisonFilter("_ownerid_value", DataverseComparisonOperator.Equal, userId),
                new DataverseComparisonFilter("gg_date", DataverseComparisonOperator.Equal, dateValue))
        };
    }
}