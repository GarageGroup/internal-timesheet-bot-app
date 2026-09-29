using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Telegram.Bot;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentActionCommandTest
{
    private static readonly Guid SomeActionId = new("78302d93-e6dc-4fd6-be63-2480c8984382");

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public static void Parse_AgentActionCallback_ExpectActualTelegramContext(int decisionCode)
    {
        var decision = (AgentActionDecision)decisionCode;
        var command = new AgentActionCommand(StubAgentActionApi.Instance);
        var user = new BotUser(202, false, "Some user");
        var chat = new BotChat(303, BotChatType.Private);
        var message = new BotMessage(404, DateTime.UtcNow, chat) { From = user };
        var callback = new BotCallbackQuery("callback", user, "instance")
        {
            Message = message,
            Data = AgentActionCommand.BuildCallbackData(SomeActionId, decision)
        };
        var update = new ChatUpdate(101, user, chat) { CallbackQuery = callback };

        var actual = command.Parse(update).OrThrow();

        Assert.Equal(new AgentActionCommandIn(101, 202, 303, 404, SomeActionId, decision), actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("other:78302d93e6dc4fd6be632480c8984382")]
    [InlineData("agent-action:z:78302d93e6dc4fd6be632480c8984382")]
    [InlineData("agent-action:c:not-a-guid")]
    public static void Parse_NotAgentActionCallback_ExpectAbsent(string? data)
    {
        var command = new AgentActionCommand(StubAgentActionApi.Instance);
        var user = new BotUser(202, false, "Some user");
        var chat = new BotChat(303, BotChatType.Private);
        var callback = new BotCallbackQuery("callback", user, "instance") { Data = data };
        var update = new ChatUpdate(101, user, chat) { CallbackQuery = callback };

        var actual = command.Parse(update);

        Assert.False(actual.IsPresent);
    }

    private sealed class StubAgentActionApi : IAgentActionApi
    {
        internal static StubAgentActionApi Instance { get; } = new();

        public ValueTask<AgentActionDecisionOut> DecideAsync(
            Guid actionId,
            long telegramUpdateId,
            long telegramUserId,
            long telegramChatId,
            AgentActionDecision decision,
            CancellationToken cancellationToken)
            =>
            throw new NotSupportedException();
    }
}
