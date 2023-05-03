using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetCreateFlow(this IBotBuilder botBuilder)
        =>
        UseTimesheetApi().MapTimesheetCreateFlow(botBuilder, TimesheetCreateCommand);
}