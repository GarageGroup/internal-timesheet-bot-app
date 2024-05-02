namespace GarageGroup.Internal.Timesheet;

internal static partial class DataverseProjectSearch
{
    internal static readonly string Filter
        = 
        $"objecttypecode ne {TimesheetProjectType.Incident:D} or statecode eq 0";

    private const string ProjectEntityName = "gg_project";

    private const string LeadEntityName = "lead";

    private const string OpportunityEntityName = "opportunity";

    private const string IncidentEntityName = "incident";
}