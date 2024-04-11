using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetUpdateFlowStep
{
    internal static ChatFlow<TimesheetUpdateFlowState> UpdateTimesheet(
        this ChatFlow<TimesheetUpdateFlowState> chatFlow, ICrmTimesheetApi timesheetApi)
        =>
        chatFlow.ForwardValue(timesheetApi.Update);

    private static ValueTask<ChatFlowJump<TimesheetUpdateFlowState>> Update(
        this ICrmTimesheetApi crmTimesheetApi,
        IChatFlowContext<TimesheetUpdateFlowState> context,
        CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            context.FlowState, cancellationToken)
        .Pipe(
            MapTimesheetUpdateIn)
        .ForwardValue(
            crmTimesheetApi.UpdateAsync)
        .OnFailure(
            failure => context.Logger.LogError(failure.SourceException,
                "Не удалось изменить timesheet. FailureMessage: {1}. FailureCode: {2}", 
                failure.FailureMessage, failure.FailureCode))
        .Map(
            _ => context.FlowState,
            ToBreakState)
        .Fold(
            ChatFlowJump.Next,
            ChatFlowJump.Break<TimesheetUpdateFlowState>);

    private static Result<TimesheetUpdateIn, Failure<TimesheetUpdateFailureCode>> MapTimesheetUpdateIn(TimesheetUpdateFlowState state)
    {
        var editedTimesheet = state.TimesheetUpdate;
        var timesheet = state.Timesheets?.FirstOrDefault(t => t.Id == editedTimesheet?.Id);

        if (editedTimesheet is null || timesheet is null)
        {
            return Failure.Create(TimesheetUpdateFailureCode.Unknown, "Возникла непредвиденная ошибка");
        }

        return new TimesheetUpdateIn(
            project: state.UpdateProject ?
            new TimesheetProjectIn
                   (id: state.ProjectId,
                    type: state.ProjectType,
                    displayName: state.ProjectName
                   ) : null,
            duration: editedTimesheet.Duration != timesheet.Duration ? editedTimesheet.Duration : null,
            description: editedTimesheet.Description != timesheet.Description ? editedTimesheet.Description : null,
            timesheetId: editedTimesheet.Id);
    }

    private static ChatFlowBreakState ToBreakState(Failure<TimesheetUpdateFailureCode> failure)
        =>
        ChatFlowBreakState.From("Ну удалось изменить запись.", failure.FailureMessage, failure.SourceException);
}