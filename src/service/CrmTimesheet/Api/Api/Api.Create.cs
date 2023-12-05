using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra;

namespace GarageGroup.Internal.Timesheet;

partial class CrmTimesheetApi
{
    public ValueTask<Result<Unit, Failure<TimesheetCreateFailureCode>>> CreateAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input ?? throw new ArgumentNullException(nameof(input)), cancellationToken)
        .Pipe(
            @in => new TimesheetJson
            {
                ProjectType = @in.Project.Type,
                ProjectId = @in.Project.Id,
                ProjectDisplayName = @in.Project.DisplayName,
                Date = @in.Date,
                Description = @in.Description.OrNullIfEmpty(),
                Duration = @in.Duration,
                ChannelCode = GetChannelCode(@in.Channel)
            }
            .BuildEntityOrFailure())
        .Map(
            static entity => new DataverseEntityCreateIn<IReadOnlyDictionary<string, object?>>(
                entityPluralName: TimesheetJson.EntityPluralName,
                entityData: entity),
            static failure => failure.WithFailureCode(TimesheetCreateFailureCode.Unknown))
        .ForwardValue(
            dataverseApi.Impersonate(input.UserId).CreateEntityAsync,
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