using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class LastTimesheetItemJson
{
    public const string EntityPluralName = "gg_timesheetactivities";

    public static readonly FlatArray<DataverseExpandedField> ExpandedFields
        =
        new(
            new(IncidentFieldName, IncidentJson.FieldNames),
            new(LeadFieldName, LeadJson.FieldNames),
            new(OpportunityFieldName, OpportunityJson.FieldNames),
            new(ProjectFieldName, ProjectJson.FieldNames));

    public static readonly FlatArray<string> SelectedFields
        =
        new(DateField);

    public static readonly FlatArray<DataverseOrderParameter> OrderFields
        =
        new(
            new(DateField, DataverseOrderDirection.Descending),
            new(CreatedOnField, DataverseOrderDirection.Descending));

    private const string IncidentFieldName = "regardingobjectid_incident";

    private const string LeadFieldName = "regardingobjectid_lead";

    private const string OpportunityFieldName = "regardingobjectid_opportunity";

    private const string ProjectFieldName = "regardingobjectid_gg_project";

    private const string DateField = "gg_date";

    private const string CreatedOnField = "createdon";

    private const string OwnerIdField = "_ownerid_value";

    [JsonPropertyName("regardingobjectid_incident")]
    public IncidentJson? Incident { get; init; }

    [JsonPropertyName("regardingobjectid_lead")]
    public LeadJson? Lead { get; init; }

    [JsonPropertyName("regardingobjectid_opportunity")]
    public OpportunityJson? Opportunity { get; init; }

    [JsonPropertyName("regardingobjectid_gg_project")]
    public ProjectJson? Project { get; init; }

    [JsonPropertyName(DateField)]
    public DateTime TimesheetDate { get; init; }

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

    internal static string BuildFilter(Guid userId, DateOnly maxDate, DateOnly? minDate)
    {
        return new DataverseLogicalFilter(DataverseLogicalOperator.And)
        {
            Filters = BuildInnerFilters().ToFlatArray<IDataverseFilter>()
        };

        IEnumerable<DataverseComparisonFilter> BuildInnerFilters()
        {
            yield return new(OwnerIdField, DataverseComparisonOperator.Equal, userId);
            yield return new("_regardingobjectid_value", DataverseComparisonOperator.Inequal, DataverseFilterValue.Null);
            yield return new(DateField, DataverseComparisonOperator.Less, FromDate(maxDate));

            if (minDate is not null)
            {
                yield return new(DateField, DataverseComparisonOperator.Greater, FromDate(minDate.Value));
            }
        }

        static DataverseFilterValue FromDate(DateOnly date)
            =>
            DataverseFilterValue.FromRawString(date.ToString("yyyy-MM-dd"));
    }
}