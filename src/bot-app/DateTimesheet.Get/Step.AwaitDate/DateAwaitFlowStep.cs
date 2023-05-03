using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateAwaitFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> AwaitDate(
        this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.AwaitTimesheetDate(
            DateAwaitHelper.GetTimesheetDateStepOption,
            WithDate);

    private static DateTimesheetFlowState WithDate(DateTimesheetFlowState state, DateOnly date)
        =>
        state with
        {
            Date = date
        };
}