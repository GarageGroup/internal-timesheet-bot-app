using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

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