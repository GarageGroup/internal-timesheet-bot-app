using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class DataverseProjectSearch
{
    internal static TimesheetProjectType? GetProjectType(this DataverseSearchItem item)
        =>
        item.EntityName switch
        {
            ProjectEntityName => TimesheetProjectType.Project,
            LeadEntityName => TimesheetProjectType.Lead,
            OpportunityEntityName => TimesheetProjectType.Opportunity,
            IncidentEntityName => TimesheetProjectType.Incident,
            _ => null
        };
}