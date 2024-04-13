using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi
{
    public ValueTask<Result<Unit, Failure<TimesheetUpdateFailureCode>>> UpdateAsync(
        TimesheetUpdateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            BuildTimesheetJsonOrFailure)
        .MapSuccess(
            timesheet => TimesheetJson.BuildDataverseUpdateInput(input.TimesheetId, timesheet))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetUpdateFailureCode));

    private Result<TimesheetJson, Failure<TimesheetUpdateFailureCode>> BuildTimesheetJsonOrFailure(TimesheetUpdateIn input)
    {
        var timesheet = new TimesheetJson
        {
            Subject = input.Project.DisplayName,
            Date = input.Date.ToDateTime(default),
            Description = input.Description.OrNullIfEmpty(),
            Duration = input.Duration
        };

        return BindProjectOrFailure<TimesheetUpdateFailureCode>(timesheet, input.Project);
    }

    private static TimesheetUpdateFailureCode ToTimesheetUpdateFailureCode(DataverseFailureCode dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetUpdateFailureCode.NotFound,
            DataverseFailureCode.Throttling => TimesheetUpdateFailureCode.TooManyRequests,
            _ => default
        };
}