using System;
using GarageGroup.Infra.Telegram.Bot;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentMessageCommandTest
{
    [Fact]
    public static void Parse_TextMessage_ExpectActualTelegramContext()
    {
        var command = new AgentMessageCommand(null!);
        var user = new BotUser(202, false, "Some user") { LanguageCode = "ru" };
        var chat = new BotChat(303, BotChatType.Private);
        var message = new BotMessage(404, DateTime.UtcNow, chat)
        {
            From = user,
            Text = "Some question"
        };
        var update = new ChatUpdate(101, user, chat) { Message = message };

        var actual = command.Parse(update).OrThrow();

        Assert.Equal(new AgentMessageCommandIn(101, 202, 303, "Some question", "ru"), actual);
    }

    [Fact]
    public static void Parse_BotCommand_ExpectAbsent()
    {
        var command = new AgentMessageCommand(null!);
        var user = new BotUser(202, false, "Some user");
        var chat = new BotChat(202, BotChatType.Private);
        var message = new BotMessage(303, DateTime.UtcNow, chat)
        {
            From = user,
            Text = "/start",
            Entities = [new(BotMessageEntityType.BotCommand, 0, 6)]
        };
        var update = new ChatUpdate(101, user, chat) { Message = message };

        var actual = command.Parse(update);

        Assert.False(actual.IsPresent);
    }
}
