using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseDateTimesheetDeleteFlow(this IBotBuilder botBuilder)
        =>
        UseCrmTimesheetApi().MapDateTimesheetDeleteFlow(botBuilder, TimesheetDeleteCommand);
}