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
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe(
            _ => new DataverseEntityDeleteIn(
            entityPluralName: TimesheetJson.EntityPluralName,
            entityKey: new DataversePrimaryKey(input.TimesheetId)))
        .PipeValue(
            dataverseApi.DeleteEntityAsync)
        .MapFailure(
            failure => failure.MapFailureCode(ToTimesheetDeleteFailureCode))
        .MapSuccess(
            Unit.From);

    private static TimesheetDeleteFailureCode ToTimesheetDeleteFailureCode(DataverseFailureCode dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetDeleteFailureCode.NotFound,
            DataverseFailureCode.Throttling => TimesheetDeleteFailureCode.TooManyRequests,
            _ => default
        };
}