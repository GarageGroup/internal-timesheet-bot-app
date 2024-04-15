using System;
using System.Text.Json.Serialization;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetJson
{
    private const string EntityPluralName
        =
        "gg_timesheetactivities";

    internal static DataverseEntityCreateIn<TimesheetJson> BuildDataverseCreateInput(TimesheetJson timesheet)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityData: timesheet);

    internal static DataverseEntityUpdateIn<TimesheetJson> BuildDataverseUpdateInput(Guid timesheetId, TimesheetJson timesheet)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId),
            entityData: timesheet);

    internal static DataverseEntityDeleteIn BuildDataverseDeleteInput(Guid timesheetId)
        =>
        new(
            entityPluralName: EntityPluralName,
            entityKey: new DataversePrimaryKey(timesheetId));

    internal static string BuildIncidentLookupValue(Guid incidentId)
        =>
        $"/incidents({incidentId:D})";

    internal static string BuildLeadLookupValue(Guid leadId)
        =>
        $"/leads({leadId:D})";

    internal static string BuildOpportunityLookupValue(Guid opportunityId)
        =>
        $"/opportunities({opportunityId:D})";

    internal static string BuildProjectLookupValue(Guid projectId)
        =>
        $"/gg_projects({projectId:D})";

    [JsonPropertyName("gg_date")]
    public DateOnly Date { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("regardingobjectid_incident@odata.bind")]
    public string? IncidentLookupValue { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("regardingobjectid_lead@odata.bind")]
    public string? LeadLookupValue { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("regardingobjectid_opportunity@odata.bind")]
    public string? OpportunityLookupValue { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("regardingobjectid_gg_project@odata.bind")]
    public string? ProjectLookupValue { get; init; }

    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("gg_description")]
    public string? Description { get; init; }

    [JsonPropertyName("gg_duration")]
    public decimal Duration { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("gg_timesheetactivity_channel")]
    public int? ChannelCode { get; init; }
}