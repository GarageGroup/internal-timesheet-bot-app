using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi
{
    public ValueTask<Result<Unit, Failure<TimesheetDeleteFailureCode>>> DeleteAsync(
        TimesheetDeleteIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input.TimesheetId, cancellationToken)
        .Pipe(
            TimesheetJson.BuildDataverseDeleteInput)
        .PipeValue(
            dataverseApi.DeleteEntityAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(ToTimesheetDeleteFailureCode));

    private static TimesheetDeleteFailureCode ToTimesheetDeleteFailureCode(DataverseFailureCode dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetDeleteFailureCode.NotFound,
            DataverseFailureCode.Throttling => TimesheetDeleteFailureCode.TooManyRequests,
            _ => default
        };
}