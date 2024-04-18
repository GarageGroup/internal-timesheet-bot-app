using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseTimesheetGetFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseCrmTimesheetApi())
        .With(
            ResolveTimesheetCreateFlowOptionOrThrow<TimesheetGetFlowOption>)
        .MapTimesheetGetFlow(
            botBuilder, DateTimesheetGetCommand);
}