using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApi<TDataverseApi>
{
    public ValueTask<Result<ProjectSetSearchOut, Failure<ProjectSetGetFailureCode>>> SearchAsync(
        ProjectSetSearchIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe<DataverseSearchIn>(
            static @in => new($"*{@in.SearchText}*")
            {
                Entities = ProjectTypeEntityNames,
                Top = @in.Top
            })
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).SearchAsync)
        .Map(
            static success => new ProjectSetSearchOut
            {
                Projects = success.Value.AsEnumerable().Select(MapProjectType).NotNull().Select(MapProjectItem).ToFlatArray()
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private static ITimesheetProjectType? MapProjectType(DataverseSearchItem item)
    {
        if (IncidentJson.FromSearchItem(item) is IncidentJson incident)
        {
            return incident;
        }

        if (LeadJson.FromSearchItem(item) is LeadJson lead)
        {
            return lead;
        }

        if (OpportunityJson.FromSearchItem(item) is OpportunityJson opportunity)
        {
            return opportunity;
        }

        if (ProjectJson.FromSearchItem(item) is ProjectJson project)
        {
            return project;
        }

        return null;
    }
}