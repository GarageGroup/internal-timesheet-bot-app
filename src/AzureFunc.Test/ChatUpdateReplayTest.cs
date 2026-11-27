using System;
using System.Text.Json;
using GarageGroup.Infra.Telegram.Bot;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class ChatUpdateReplayTest
{
    // The update that required a sign-in is stored as JSON and sent to the bot again after the sign-in
    [Fact]
    public static void ChatUpdate_SerializedAndDeserialized_ExpectMessageUserAndChatArePreserved()
    {
        var user = new BotUser(id: 15, isBot: false, firstName: "Ivan")
        {
            LanguageCode = "ru"
        };

        var chat = new BotChat(id: 27, type: BotChatType.Private);

        var source = new ChatUpdate(updateId: 101, user: user, chat: chat)
        {
            Message = new(messageId: 7, date: new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc), chat: chat)
            {
                From = user,
                Text = "сколько я списал сегодня?"
            }
        };

        var actual = JsonSerializer.Deserialize<ChatUpdate>(JsonSerializer.Serialize(source));

        Assert.NotNull(actual);
        Assert.Equal(101, actual.UpdateId);
        Assert.Equal(15, actual.User.Id);
        Assert.Equal("ru", actual.User.LanguageCode);
        Assert.Equal(27, actual.Chat.Id);
        Assert.Equal("сколько я списал сегодня?", actual.Message?.Text);
        Assert.Equal(15, actual.Message?.From?.Id);
    }
}
