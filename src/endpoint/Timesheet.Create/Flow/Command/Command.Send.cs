using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class TimesheetCreateCommand
{
    public ValueTask<ChatCommandResult<Unit>> SendAsync(
        ChatCommandRequest<TimesheetCreateCommandIn, Unit> request, CancellationToken cancellationToken)
        =>
        request.StartChatFlow(
            @in => new TimesheetCreateFlowState(
                limitationDayOfMonth: option.LimitationDayOfMonth,
                userId: request.Context.User.Identity?.SystemId ?? default)
            {
                TimesheetId = @in.TimesheetId,
                Description = @in.Description is null ? null : new(@in.Description),
                Duration = @in.Duration,
                Project = @in.Project is null ? null : new()
                {
                    Id = @in.Project.Id,
                    Name = @in.Project.Name,
                    Type = @in.Project.Type
                },
                Date = @in.Date,
                ShowSelectedDate = @in.Date is not null && @in.TimesheetId is null,
                WithoutConfirmation = @in.Project is not null
            })
        .ExpectDateOrSkip()
        .ExpectProjectOrSkip(
            crmProjectApi)
        .ExpectDurationOrSkip()
        .ExpectDescriptionOrSkip(
            crmTimesheetApi)
        .ExpectConfirmationOrSkip()
        .CreateOrUpdateTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheets()
        .GetResultAsync(
            Unit.From, cancellationToken);
}