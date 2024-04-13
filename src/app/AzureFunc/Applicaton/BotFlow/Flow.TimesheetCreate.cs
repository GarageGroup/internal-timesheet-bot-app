using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetCreateFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseCrmProjectApi())
        .With(
            UseCrmTimesheetApi())
        .With(
            ResolveTimesheetEditOptionOrThrow<TimesheetEditOption>)
        .MapTimesheetCreateFlow(botBuilder, TimesheetCreateCommand);
}