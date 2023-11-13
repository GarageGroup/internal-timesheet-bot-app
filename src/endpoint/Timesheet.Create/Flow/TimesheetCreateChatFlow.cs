using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> RunFlow(
        this ChatFlow chatFlow,
        IBotContext botContext,
        ICrmProjectApi crmProjectApi,
        ICrmTimesheetApi crmTimesheetApi)
        =>
        chatFlow.Start<TimesheetCreateFlowState>(
            static () => new())
        .GetUserId()
        .AwaitProject(
            crmProjectApi)
        .AwaitDate()
        .AwaitHourValue()
        .AwaitDescription()
        .ConfirmTimesheet()
        .CreateTimesheet(
            crmTimesheetApi)
        .ShowDateTimesheet(
            botContext);
}