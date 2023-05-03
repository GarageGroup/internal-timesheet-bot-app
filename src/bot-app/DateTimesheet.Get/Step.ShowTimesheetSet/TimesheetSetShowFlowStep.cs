using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class TimesheetSetShowFlowStep
{
    internal static ChatFlow<Unit> ShowTimesheetSet(this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.ReplaceActivityOrSkip(
            TimesheetSetShowHelper.CreateActivity)
        .MapFlowState(
            Unit.From);
}