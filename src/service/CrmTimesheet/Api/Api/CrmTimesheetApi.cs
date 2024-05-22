using System;
using System.Text.RegularExpressions;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

using TSqlApi = ISqlQueryEntitySetSupplier;

internal sealed partial class CrmTimesheetApi(IDataverseApiClient dataverseApi, TSqlApi sqlApi) : ICrmTimesheetApi
{
    private const string TagStartSymbol = "#";

    private const int TelegramChannelCode = 140120000;

    private static readonly Regex TagRegex;

    private static readonly IDbFilter DescriptionTagFilter;

    static CrmTimesheetApi()
    {
        TagRegex = CreateTagRegex();
        DescriptionTagFilter = DbTimesheetTag.BuildDescriptionFilter(TagStartSymbol);
    }

    [GeneratedRegex($"{TagStartSymbol}\\w+", RegexOptions.CultureInvariant)]
    private static partial Regex CreateTagRegex();

    private static Result<TimesheetJson, Failure<TFailureCode>> BindProjectOrFailure<TFailureCode>(
        TimesheetJson timesheet, TimesheetProject project)
        where TFailureCode : struct
    {
        if (project.Type is TimesheetProjectType.Project)
        {
            return timesheet with
            {
                ProjectLookupValue = TimesheetJson.BuildProjectLookupValue(project.Id)
            };
        }

        if (project.Type is TimesheetProjectType.Opportunity)
        {
            return timesheet with
            {
                OpportunityLookupValue = TimesheetJson.BuildOpportunityLookupValue(project.Id)
            };
        }

        if (project.Type is TimesheetProjectType.Lead)
        {
            return timesheet with
            {
                LeadLookupValue = TimesheetJson.BuildLeadLookupValue(project.Id)
            };
        }

        if (project.Type is TimesheetProjectType.Incident)
        {
            return timesheet with
            {
                IncidentLookupValue = TimesheetJson.BuildIncidentLookupValue(project.Id)
            };
        }

        return Failure.Create<TFailureCode>(default, $"An unexpected project type: {project.Type}");
    }
}