using System;
using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

using ITimesheetSetGetFunc = IAsyncValueFunc<TimesheetSetGetIn, Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>>;

partial class TimesheetSetGetChatFlow
{
    internal static ChatFlow<Unit> GetTimesheet(
        this ChatFlow chatFlow,
        IBotUserProvider botUserProvider,
        ITimesheetSetGetFunc timesheetSetGetFunc)
        =>
        chatFlow.Start(
            static () => new DateTimesheetFlowState())
        .GetDate()
        .GetTimesheet(
            botUserProvider,
            timesheetSetGetFunc);
}