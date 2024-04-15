using System;
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
            input, cancellationToken)
        .Pipe(
            BuildTimesheetJsonOrFailure)
        .MapSuccess(
            TimesheetJson.BuildDataverseCreateInput)
        .ForwardValue(
            dataverseApi.Impersonate(input.UserId).CreateEntityAsync,
            static failure => failure.MapFailureCode(ToTimesheetCreateFailureCode));

    private Result<TimesheetJson, Failure<TimesheetCreateFailureCode>> BuildTimesheetJsonOrFailure(TimesheetCreateIn input)
    {
        var timesheet = new TimesheetJson
        {
            Subject = input.Project.DisplayName,
            Date = input.Date,
            Description = input.Description.OrNullIfEmpty(),
            Duration = input.Duration,
            ChannelCode = GetChannelCode(@input.Channel)
        };

        return BindProjectOrFailure<TimesheetCreateFailureCode>(timesheet, input.Project);

        int? GetChannelCode(TimesheetChannel channel)
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