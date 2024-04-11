using System;
using System.Collections.Generic;
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
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe(
            input => new UpdateTimesheetJson
            {
                Description = input.Description,
                Duration = input.Duration,
                Id = input.TimesheetId,
                ProjectId = input.Project?.Id,
                ProjectType = input.Project?.Type,
                ProjectDisplayName = input.Project?.DisplayName,
            }
            .BuildEntityOrFailure())
        .Map(
            @in => new DataverseEntityUpdateIn<IReadOnlyDictionary<string, object?>>(
                entityPluralName: TimesheetJson.EntityPluralName,
                entityKey: new DataversePrimaryKey(input.TimesheetId),
                entityData: @in),
            static failure => failure.WithFailureCode(TimesheetUpdateFailureCode.Unknown))
        .ForwardValue(
            dataverseApi.UpdateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetUpdateFailureCode));


    private static TimesheetUpdateFailureCode ToTimesheetUpdateFailureCode(DataverseFailureCode dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetUpdateFailureCode.NotFound,
            DataverseFailureCode.Throttling => TimesheetUpdateFailureCode.TooManyRequests,
            _ => default
        };
}