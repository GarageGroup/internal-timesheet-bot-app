using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApi
{
    public ValueTask<Result<ProjectSetSearchOut, Failure<ProjectSetGetFailureCode>>> SearchAsync(
        ProjectSetSearchIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe<DataverseSearchIn>(
            static @in => new($"*{@in.SearchText}*")
            {
                Top = @in.Top,
                Entities = DataverseProjectSearch.EntityNames
            })
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).SearchAsync)
        .Map(
            static success => new ProjectSetSearchOut
            {
                Projects = GetProjects(success.Value).ToFlatArray()
            },
            static failure => failure.MapFailureCode(MapFailureCode));

    private static IEnumerable<ProjectSetGetItem> GetProjects(FlatArray<DataverseSearchItem> items)
    {
        foreach (var item in items)
        {
            var projectType = item.GetProjectType();
            if (projectType is null)
            {
                continue;
            }

            yield return new(
                id: item.ObjectId,
                name: item.GetProjectName(projectType.Value),
                type: projectType.Value);
        }
    }

    private static ProjectSetGetFailureCode MapFailureCode(DataverseFailureCode failureCode)
        =>
        failureCode switch
        {
            DataverseFailureCode.UserNotEnabled => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.PrivilegeDenied => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.SearchableEntityNotFound => ProjectSetGetFailureCode.NotAllowed,
            DataverseFailureCode.Throttling => ProjectSetGetFailureCode.TooManyRequests,
            _ => default
        };
}