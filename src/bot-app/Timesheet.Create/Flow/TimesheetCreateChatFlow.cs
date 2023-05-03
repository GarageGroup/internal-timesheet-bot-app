using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

internal static partial class TimesheetCreateChatFlow
{
    internal static ChatFlow<Unit> RunFlow<TTimesheetApi>(this ChatFlow chatFlow, IBotContext botContext, TTimesheetApi timesheetApi)
        where TTimesheetApi : IFavoriteProjectSetGetSupplier, IProjectSetSearchSupplier, ITimesheetCreateSupplier
        =>
        chatFlow.Start<TimesheetCreateFlowState>(
            static () => new())
        .GetUserId()
        .AwaitProject(
            timesheetApi)
        .AwaitDate()
        .AwaitHourValue()
        .AwaitDescription()
        .ConfirmTimesheet()
        .CreateTimesheet(
            timesheetApi)
        .ShowDateTimesheet(
            botContext);
}