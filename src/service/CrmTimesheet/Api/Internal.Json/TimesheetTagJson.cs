using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetTagJson
{
    private const string DescriptionFieldName = "gg_description";

    private const string DescriptionContainsTagQuery = $"contains({DescriptionFieldName}, '%23')";

    public static readonly FlatArray<string> SelectedFields
        =
        new(DescriptionFieldName);

    public static readonly FlatArray<DataverseOrderParameter> OrderFields
        =
        new DataverseOrderParameter("createdon", DataverseOrderDirection.Descending).AsFlatArray();

    [JsonPropertyName(DescriptionFieldName)]
    public string? Description { get; init; }

    internal static string BuildFilter(Guid userId, Guid projectId, DateOnly minDate)
    {
        var dateValue = DataverseFilterValue.FromRawString(minDate.ToString("yyyy-MM-dd"));

        return new DataverseLogicalFilter(DataverseLogicalOperator.And)
        {
            Filters = new(
                new DataverseComparisonFilter("_ownerid_value", DataverseComparisonOperator.Equal, userId),
                new DataverseComparisonFilter("_regardingobjectid_value", DataverseComparisonOperator.Equal, projectId),
                DescriptionContainsTagFilter.Instance,
                new DataverseComparisonFilter("gg_date", DataverseComparisonOperator.GreaterOrEqual, dateValue))
        };
    }

    private sealed record class DescriptionContainsTagFilter : DataverseFilterBase
    {
        internal static readonly DescriptionContainsTagFilter Instance = new();

        public override string GetQuery()
            =>
            DescriptionContainsTagQuery;
    }
}