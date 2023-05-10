using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseDateTimesheetGetFlow(this IBotBuilder botBuilder)
        =>
        UseTimesheetApi().MapDateTimesheetGetFlow(botBuilder, DateTimesheetGetCommand);
}