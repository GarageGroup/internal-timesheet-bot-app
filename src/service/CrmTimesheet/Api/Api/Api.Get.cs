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
            static @in => new DataverseEntitySetGetIn(
                entityPluralName: BaseTimesheetItemJson.EntityPluralName,
                selectFields: TimesheetItemJson.SelectedFields,
                expandFields: BaseTimesheetItemJson.ExpandedFields,
                filter: TimesheetItemJson.BuildFilter(@in.UserId, @in.Date),
                orderBy: TimesheetItemJson.OrderFiels))
        .PipeValue(
            dataverseApi.Impersonate(input.UserId).GetEntitySetAsync<TimesheetItemJson>)
        .MapFailure(
            static failure => failure.MapFailureCode(ToTimesheetSetGetFailureCode))
        .MapSuccess(
            static @out => new TimesheetSetGetOut
            {
                Timesheets = @out.Value.Map(MapTimesheetItemJson)
            });

    private static TimesheetSetGetItem MapTimesheetItemJson(TimesheetItemJson itemJson)
        =>
        new(
            timesheetId: itemJson.TimesheetId,
            date: new(itemJson.Date.Year, itemJson.Date.Month, itemJson.Date.Day),
            duration: itemJson.Duration,
            projectName: itemJson.GetProjectType()?.Name, 
            description: itemJson.Description);

    private static TimesheetSetGetFailureCode ToTimesheetSetGetFailureCode(DataverseFailureCode code)
        =>
        code switch
        {
            DataverseFailureCode.UserNotEnabled => TimesheetSetGetFailureCode.NotAllowed,
            DataverseFailureCode.PrivilegeDenied => TimesheetSetGetFailureCode.NotAllowed,
            DataverseFailureCode.Throttling => TimesheetSetGetFailureCode.TooManyRequests,
            _ => default
        };
}