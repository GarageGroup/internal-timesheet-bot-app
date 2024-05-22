using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static BotCommandBuilder WithTimesheetShowCommand(this BotCommandBuilder builder)
        =>
        builder.With(
            "datetimesheet", UseTimesheetShowCommand());

    private static Dependency<IChatCommand<TimesheetShowCommandIn, Unit>> UseTimesheetShowCommand()
        =>
        Pipeline.Pipe(
            UseCrmTimesheetApi())
        .With(
            ResolveTimesheetCreateFlowOptionOrThrow<TimesheetShowFlowOption>)
        .UseTimesheetShowCommand();
}