using System;
using System.Text;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetItemJson : BaseTimesheetItemJson
{
    public static readonly FlatArray<string> SelectedFields
        =
        new(DateFieldName, DurationFieldName, DescriptionFieldName);

    public static readonly FlatArray<DataverseOrderParameter> OrderFiels
        =
        new DataverseOrderParameter("createdon", DataverseOrderDirection.Ascending).AsFlatArray();

    private const string DateFieldName = "gg_date";

    private const string DurationFieldName = "gg_duration";

    private const string DescriptionFieldName = "gg_description";

    private const string ActivityIdFieldName = "activityid";

    [JsonPropertyName(ActivityIdFieldName)]
    public Guid TimesheetId { get; init; }

    [JsonPropertyName(DateFieldName)]
    public DateTimeOffset Date { get; init; }

    [JsonPropertyName(DurationFieldName)]
    public decimal Duration { get; init; }

    [JsonPropertyName(DescriptionFieldName)]
    public string? Description { get; init; }

    internal static string BuildFilter(Guid userId, DateOnly date)
        =>
        new StringBuilder(
            $"_ownerid_value eq '{userId}'")
        .Append(
            " and ")
        .Append(
            $"gg_date eq {date:yyyy-MM-dd}")
        .ToString();
}