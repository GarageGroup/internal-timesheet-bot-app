using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi<TDataverseApi>
{
    public ValueTask<Result<Unit, Failure<TimesheetCreateFailureCode>>> CreateAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe(
            @in => new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                entityPluralName: TimesheetItemJson.EntityPluralName,
                entityData: new TimesheetJsonCreateIn
                {
                    ProjectType = @in.ProjectType,
                    ProjectId = @in.ProjectId,
                    Date = @in.Date,
                    Description = @in.Description.OrNullIfEmpty(),
                    Duration = @in.Duration,
                    ChannelCode = GetChannelCode(@in.Channel)
                }
                .BuildEntity()))
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).CreateEntityAsync)
        .MapFailure(
            static failure => failure.MapFailureCode(ToTimesheetCreateFailureCode));

    private int? GetChannelCode(TimesheetChannel channel)
    {
        foreach (var channelCode in option.ChannelCodes)
        {
            if (channelCode.Key == channel)
            {
                return channelCode.Value;
            }
        }

        return null;
    }

    private static TimesheetCreateFailureCode ToTimesheetCreateFailureCode(DataverseFailureCode dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            DataverseFailureCode.RecordNotFound => TimesheetCreateFailureCode.NotFound,
            DataverseFailureCode.UserNotEnabled => TimesheetCreateFailureCode.NotAllowed,
            DataverseFailureCode.PrivilegeDenied => TimesheetCreateFailureCode.NotAllowed,
            DataverseFailureCode.Throttling => TimesheetCreateFailureCode.TooManyRequests,
            _ => default
        };
}