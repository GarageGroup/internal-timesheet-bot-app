using System;
using System.Text;
using System.Text.Json.Serialization;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class LeadJson : ITimesheetProjectType
{
    public static readonly FlatArray<string> FieldNames = new(SubjectFieldName, CompanyNameFieldName);

    public const TimesheetProjectType Type = TimesheetProjectType.Lead;

    public const string EntityName = "lead";

    public const string EntitySetName = "leads";

    private const string IdFieldName = "leadid";

    private const string SubjectFieldName = "subject";

    private const string CompanyNameFieldName = "companyname";

    [JsonPropertyName(IdFieldName)]
    public Guid Id { get; init; }

    [JsonPropertyName(SubjectFieldName)]
    public string? Subject { get; init; }

    [JsonPropertyName(CompanyNameFieldName)]
    public string? CompanyName { get; init; }

    string? ITimesheetProjectType.Name
    {
        get
        {
            if (string.IsNullOrEmpty(CompanyName))
            {
                return Subject;
            }

            var stringBuilder = new StringBuilder(Subject);
            if(string.IsNullOrEmpty(Subject) is false)
            {
                stringBuilder.Append(' ');
            }
        
            return stringBuilder.Append('(').Append(CompanyName).Append(')').ToString();
        }
    }

    TimesheetProjectType ITimesheetProjectType.Type
        =>
        Type;
}