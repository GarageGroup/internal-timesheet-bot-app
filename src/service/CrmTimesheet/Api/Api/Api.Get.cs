using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi<TDataverseApi>
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
                    Filters = new(
                        DbTimesheet.BuildOwnerFilter(@in.UserId),
                        DbTimesheet.BuildDateFilter(@in.Date))
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
            static failure => failure.MapFailureCode(GetUnknownSetGetFailureCode));

    private static TimesheetSetGetItem MapTimesheet(DbTimesheet dbTimesheet)
        =>
        new(
            duration: dbTimesheet.Duration,
            projectName: dbTimesheet.Subject.OrNullIfEmpty() ?? dbTimesheet.ProjectName, 
            description: dbTimesheet.Description);

    private static TimesheetSetGetFailureCode GetUnknownSetGetFailureCode(Unit _)
        =>
        TimesheetSetGetFailureCode.Unknown;
}