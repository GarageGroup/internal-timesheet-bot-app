using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateTimesheetShowFlowStep
{
    internal static ChatFlow<Unit> ShowDateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, IBotContext botContext)
        =>
        chatFlow.Next(
            botContext.RunDateTimesheetCommandAsync);
}