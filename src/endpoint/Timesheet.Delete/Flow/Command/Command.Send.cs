using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetDeleteCommand
{
    public ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<TimesheetDeleteCommandIn, Unit> request, CancellationToken cancellationToken)
        =>
        request.StartChatFlow(
            static @in => new TimesheetDeleteFlowState
            {
                TimesheetId = @in.TimesheetId,
                Date = @in.Date,
                ProjectName = @in.ProjectName,
                ProjectTypeDisplayName = @in.ProjectTypeDisplayName,
                Duration = @in.Duration,
                Description = @in.Description
            })
        .ExpectDeletionConfirmation()
        .DeleteTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheets()
        .GetResultAsync(
            Unit.From, cancellationToken);
}
