using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DateGetFlowStep
{
    internal static ChatFlow<TimesheetCreateFlowState> GetDate(this ChatFlow<TimesheetCreateFlowState> chatFlow)
        =>
        chatFlow.AwaitTimesheetDate("Дата списания", 2, WithDate);

    private static TimesheetCreateFlowState WithDate(TimesheetCreateFlowState state, DateOnly date)
        =>
        state with
        {
            Date = date
        };
}