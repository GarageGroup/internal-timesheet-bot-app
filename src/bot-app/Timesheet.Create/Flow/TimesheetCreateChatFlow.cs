using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

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