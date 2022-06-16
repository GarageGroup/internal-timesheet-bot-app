using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<DateTimesheetFlowState> GetDate(
        this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.AwaitTimesheetDate("Дата", 3, WithDate);

    private static DateTimesheetFlowState WithDate(DateTimesheetFlowState state, DateOnly date)
        =>
        state with
        {
            Date = date
        };
}