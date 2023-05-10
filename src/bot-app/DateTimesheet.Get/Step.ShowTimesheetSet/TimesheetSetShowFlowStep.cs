using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

internal static class TimesheetSetShowFlowStep
{
    internal static ChatFlow<Unit> ShowTimesheetSet(this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.ReplaceActivityOrSkip(
            TimesheetSetShowHelper.CreateActivity)
        .MapFlowState(
            Unit.From);
}