using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow,
        IBotContext botContext,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi,
        TimesheetEditOption option)
        =>
        chatFlow.Start<TimesheetCreateFlowState>(
            () => new()
            {
                UrlWebApp = option.UrlWebApp
            })
        .GetUserId()
        .AwaitProject(
            crmProjectApi)
        .AwaitDate()
        .AwaitHourValue()
        .AwaitDescription(
            crmTimesheetApi)
        .ConfirmTimesheet()
        .CreateTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheet(
            botContext);
}