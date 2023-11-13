using System;
using System.Text;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class LastTimesheetItemJson : BaseTimesheetItemJson
{
    public static readonly FlatArray<string> SelectedFields
        =
        new(DateField);

    public static readonly FlatArray<DataverseOrderParameter> OrderFiels
        =
        new(
            new(DateField, DataverseOrderDirection.Descending),
            new(CreatedOnField, DataverseOrderDirection.Descending));

    private const string DateField = "gg_date";

    private const string CreatedOnField = "createdon";

    private const string OwnerIdField = "_ownerid_value";

    [JsonPropertyName(DateField)]
    public DateTime TimesheetDate { get; init; }

    internal static string BuildFilter(Guid userId, DateOnly maxDate, DateOnly? minDate)
    {
        var filterBuilder = new StringBuilder()
            .Append($"{OwnerIdField} eq '{userId}'")
            .Append($" and _regardingobjectid_value ne null")
            .Append($" and {DateField} lt {maxDate:yyyy-MM-dd}");

        if (minDate is not null)
        {
            filterBuilder = filterBuilder.Append($" and {DateField} gt {minDate.Value:yyyy-MM-dd}");
        }

        return filterBuilder.ToString();
    }
}