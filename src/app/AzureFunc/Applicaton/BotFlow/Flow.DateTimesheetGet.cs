using System;
using GarageGroup.Infra.Bot.Builder;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static IBotBuilder UseDateTimesheetGetFlow(this IBotBuilder botBuilder)
        =>
        Pipeline.Pipe(
            UseCrmTimesheetApi())
        .With(
            ResolveTimesheetEditOptionOrThrow<DateTimesheetEditOption>)
        .MapDateTimesheetGetFlow(
            botBuilder, DateTimesheetGetCommand);
}