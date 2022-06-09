using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static class DrawStep
{
    internal static ChatFlow<Unit> DrawTimesheetSet(this ChatFlow<DateTimesheetFlowState> chatFlow)
        =>
        chatFlow.SendActivity(DrawActivity.CreateActivity).MapFlowState(Unit.From);
}