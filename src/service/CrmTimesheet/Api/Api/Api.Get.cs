using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi
{
    public ValueTask<Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>> GetAsync(
        TimesheetSetGetIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            static @in => DbTimesheet.QueryAll with
            {
                Filter = new DbCombinedFilter(DbLogicalOperator.And)
                {
                    Filters =
                    [
                        DbTimesheet.BuildOwnerFilter(@in.UserId),
                        DbTimesheet.BuildDateFilter(@in.Date),
                        DbTimesheet.BuildAllowedProjectTypeSetFilter()
                    ]
                },
                Orders = DbTimesheet.DefaultOrders
            })
        .PipeValue(
            sqlApi.QueryEntitySetOrFailureAsync<DbTimesheet>)
        .Map(
            static success => new TimesheetSetGetOut
            {
                Timesheets = success.Map(MapTimesheet)
            },
            static failure => failure.WithFailureCode(TimesheetSetGetFailureCode.Unknown));

    private static TimesheetSetGetItem MapTimesheet(DbTimesheet dbTimesheet)
        =>
        new(
            duration: dbTimesheet.Duration,
            projectType: (TimesheetProjectType)dbTimesheet.ProjectTypeCode,
            projectName: dbTimesheet.Subject.OrNullIfEmpty() ?? dbTimesheet.ProjectName, 
            description: dbTimesheet.Description,
            id: dbTimesheet.Id);
}