using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetShowCommand
{
    public ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<TimesheetShowCommandIn, Unit> request, CancellationToken cancellationToken)
        =>
        request.StartChatFlow(
            @in => new TimesheetShowFlowState(
                limitationDayOfMonth: option.LimitationDayOfMonth,
                userId: request.Context.User.Identity?.SystemId ?? default)
            {
                Date = @in.Date,
                MessageText = @in.MessageText
            })
        .ExpectDate()
        .GetTimesheetSet(
            timesheetApi)
        .ShowTimesheetSet()
        .GetResultAsync(
            Unit.From, cancellationToken);
}