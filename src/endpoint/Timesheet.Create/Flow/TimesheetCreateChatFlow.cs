using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<TimesheetCreateFlowState> RunFlow(
        this ChatFlowStarter<TimesheetCreateFlowState> chatFlow,
        IBotContext botContext,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi,
        TimesheetCreateFlowOption option,
        WebAppDataTimesheetUpdateJson? data)
        =>
        chatFlow.Start(
            () => new()
            {
                TimesheetId = data?.Id,
                Description = data is null ? null : data.Description is null ? null : new(data.Description),
                ValueHours = data?.Duration,
                Project = data?.Project,
                Date = data?.Date,
                ShowSelectedDate = data?.Date is not null && data?.Id is null,
                LimitationDay = option.LimitationDay,
                UrlWebApp = option.UrlWebApp,
                WithoutConfirmation = data?.Project is not null
            })
        .GetUserId()
        .AwaitDate()
        .AwaitProject(
            crmProjectApi)
        .AwaitDuration()
        .AwaitDescription(
            crmTimesheetApi)
        .ConfirmTimesheet()
        .CreateOrUpdateTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheet(
            botContext);
}