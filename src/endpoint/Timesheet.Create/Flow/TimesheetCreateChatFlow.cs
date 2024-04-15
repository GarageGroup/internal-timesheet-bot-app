using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<TimesheetCreateFlowState> RunFlow(
        this ChatFlowStarter<TimesheetCreateFlowState> chatFlow,
        IBotContext botContext,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi,
        TimesheetEditOption option,
        WebAppUpdateTimesheetDataJson? timesheetData)
        =>
        chatFlow.Start(
            () => new()
            {
                TimesheetId = timesheetData?.Id,
                Description = timesheetData is null ? null : new(timesheetData.Description),
                ValueHours = timesheetData?.Duration,
                Project = timesheetData?.Project,
                Date = timesheetData?.Date,
                AllowedIntervalInDays = option.AllowedIntervalInDays,
                UrlWebApp = option.UrlWebApp
            })
        .GetUserId()
        .AwaitDate()
        .AwaitProject(
            crmProjectApi)
        .AwaitHourValue()
        .AwaitDescription(
            crmTimesheetApi)
        .ConfirmTimesheet()
        .CreateOrUpdateTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheet(
            botContext);
}