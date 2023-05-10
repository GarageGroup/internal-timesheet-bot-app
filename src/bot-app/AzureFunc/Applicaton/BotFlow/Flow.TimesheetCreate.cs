using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetCreateFlow(this IBotBuilder botBuilder)
        =>
        UseTimesheetApi().MapTimesheetCreateFlow(botBuilder, TimesheetCreateCommand);
}