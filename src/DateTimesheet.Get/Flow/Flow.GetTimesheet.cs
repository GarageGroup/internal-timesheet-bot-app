using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

partial class TimesheetSetGetChatFlow
{
    internal static ChatFlow<Unit> GetTimesheet(this ChatFlow chatFlow, ITimesheetSetGetFunc timesheetSetGetFunc)
        =>
        chatFlow.Start<DateTimesheetFlowState>(
            static () => new())
        .GetUserId()
        .GetDate()
        .GetTimesheetSet(
            timesheetSetGetFunc)
        .DrawTimesheetSet();
}