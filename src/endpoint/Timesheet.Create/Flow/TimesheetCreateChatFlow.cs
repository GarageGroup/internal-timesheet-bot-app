using GarageGroup.Infra.Bot.Builder;
using GarageGroup.Internal.Timesheet.Internal.Json;
using System;

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
                UrlWebApp = option.UrlWebApp,
                TimesheetId = timesheetData?.Id,
                Description = timesheetData is not null ? new(timesheetData.Description) : null,
                ValueHours = timesheetData?.Duration,
                Project = timesheetData?.IsEditProject is false ? new()
                {
                    Type = timesheetData.ProjectType,
                    Name = timesheetData.ProjectName
                } : null,
                UpdateProject = timesheetData is not null ? timesheetData.IsEditProject : false,
                Date = timesheetData is not null ? DateOnly.Parse((timesheetData.Date).OrEmpty()) : null,
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