using GarageGroup.Infra.Bot.Builder;
using System;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetUpdateFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseCrmProjectApi())
        .With(
            UseCrmTimesheetApi())
        .MapTimesheetUpdateFlow(botBuilder, TimesheetUpdateCommand);
}