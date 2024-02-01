using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApi
{
    public ValueTask<Result<LastProjectSetGetOut, Failure<ProjectSetGetFailureCode>>> GetLastAsync(
        LastProjectSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => DbTimesheetProject.QueryAll with
            {
                Top = @in.Top,
                Filter = new DbCombinedFilter(DbLogicalOperator.And)
                {
                    Filters =
                    [
                        DbTimesheetProject.BuildOwnerFilter(@in.UserId),
                        DbTimesheetProject.BuildMinDateFilter(@in.MinDate),
                        AllowedProjectTypeSetFilter
                    ]
                },
                Orders = DbTimesheetProject.DefaultOrders
            })
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbTimesheetProject>)
        .Map(
            static success => new LastProjectSetGetOut
            {
                Projects = success.Map(MapProject)
            },
            static failure => failure.WithFailureCode(ProjectSetGetFailureCode.Unknown));

    private static ProjectSetGetItem MapProject(DbTimesheetProject dbTimesheetProject)
        =>
        new(
            id: dbTimesheetProject.ProjectId,
            name: dbTimesheetProject.Subject.OrNullIfEmpty() ?? dbTimesheetProject.ProjectName,
            type: (TimesheetProjectType)dbTimesheetProject.ProjectTypeCode);
}