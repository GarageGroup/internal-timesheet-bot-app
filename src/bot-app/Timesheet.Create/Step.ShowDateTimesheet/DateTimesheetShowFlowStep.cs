using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class DateTimesheetShowFlowStep
{
    internal static ChatFlow<Unit> ShowDateTimesheet(
        this ChatFlow<TimesheetCreateFlowState> chatFlow, IBotContext botContext)
        =>
        chatFlow.Next(
            botContext.RunDateTimesheetCommandAsync);
}