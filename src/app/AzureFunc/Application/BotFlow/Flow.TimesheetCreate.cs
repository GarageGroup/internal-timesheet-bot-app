using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static BotCommandBuilder WithTimesheetCreateCommand(this BotCommandBuilder builder)
        =>
        builder.With(
            "newtimesheet", UseTimesheetCreateCommand());

    private static Dependency<IChatCommand<TimesheetCreateCommandIn, Unit>> UseTimesheetCreateCommand()
        =>
        Pipeline.Pipe(
            UseCrmTimesheetApi())
        .With(
            UseCrmProjectApi())
        .With(
            ResolveTimesheetCreateFlowOptionOrThrow<TimesheetCreateFlowOption>)
        .UseTimesheetCreateCommand();
}