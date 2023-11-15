using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmProjectApi<TDataverseApi>
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
                    Filters = new(
                        DbTimesheetProject.BuildOwnerFilter(@in.UserId),
                        DbTimesheetProject.BuildMinDateFilter(@in.MinDate),
                        AllowedProjectTypeSetFilter)
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
            static failure => failure.MapFailureCode(GetUnknownFailureCode));

    private static ProjectSetGetItem MapProject(DbTimesheetProject dbTimesheetProject)
        =>
        new(
            id: dbTimesheetProject.ProjectId,
            name: dbTimesheetProject.Subject.OrNullIfEmpty() ?? dbTimesheetProject.ProjectName,
            type: (TimesheetProjectType)dbTimesheetProject.ProjectTypeCode);

    private static ProjectSetGetFailureCode GetUnknownFailureCode(Unit _)
        =>
        ProjectSetGetFailureCode.Unknown;
}