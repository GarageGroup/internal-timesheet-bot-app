using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static BotCommandBuilder WithBotMenuCommand(this BotCommandBuilder builder)
        =>
        builder.With(
            BotCommand.UseMenuCommand());
}
