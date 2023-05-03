using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

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