using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class DateAwaitFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> AwaitDate(this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitTimesheetDate(
            DateAwaitHelper.GetTimesheetDateStepOption,
            WithDate);

    private static TimesheetCreateFlowState WithDate(TimesheetCreateFlowState state, DateOnly date)
        =>
        state with
        {
            Date = date
        };
}