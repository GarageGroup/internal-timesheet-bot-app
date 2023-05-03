using GGroupp.Infra.Bot.Builder;

namespace GGroupp.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseDateTimesheetGetFlow(this IBotBuilder botBuilder)
        =>
        UseTimesheetApi().MapDateTimesheetGetFlow(botBuilder, DateTimesheetGetCommand);
}