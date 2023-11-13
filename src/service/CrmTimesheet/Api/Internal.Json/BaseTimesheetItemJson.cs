using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal abstract record class BaseTimesheetItemJson
{
    public const string EntityPluralName = "gg_timesheetactivities";

    public static readonly FlatArray<DataverseExpandedField> ExpandedFields
        =
        new(
            new(IncidentFieldName, IncidentJson.FieldNames),
            new(LeadFieldName, LeadJson.FieldNames),
            new(OpportunityFieldName, OpportunityJson.FieldNames),
            new(ProjectFieldName, ProjectJson.FieldNames));

    private const string IncidentFieldName = "regardingobjectid_incident";

    private const string LeadFieldName = "regardingobjectid_lead";

    private const string OpportunityFieldName = "regardingobjectid_opportunity";

    private const string ProjectFieldName = "regardingobjectid_gg_project";

    [JsonPropertyName("regardingobjectid_incident")]
    public IncidentJson? Incident { get; init; }

    [JsonPropertyName("regardingobjectid_lead")]
    public LeadJson? Lead { get; init; }

    [JsonPropertyName("regardingobjectid_opportunity")]
    public OpportunityJson? Opportunity { get; init; }

    [JsonPropertyName("regardingobjectid_gg_project")]
    public ProjectJson? Project { get; init; }

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
}