using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static BotCommandBuilder WithTimesheetDeleteCommand(this BotCommandBuilder builder)
        =>
        builder.With(
            UseTimesheetDeleteCommand());

    private static Dependency<IChatCommand<TimesheetDeleteCommandIn, Unit>> UseTimesheetDeleteCommand()
        =>
        UseCrmTimesheetApi().UseTimesheetDeleteCommand();
}