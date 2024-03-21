using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetDeleteFlow(this IBotBuilder botBuilder)
        =>
        UseCrmTimesheetApi().MapTimesheetDeleteFlow(botBuilder, TimesheetDeleteCommand);
}