using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    private static BotCommandBuilder WithBotInfoCommand(this BotCommandBuilder builder)
        =>
        builder.With(
            "info", BotCommand.UseBotInfoCommand());
}