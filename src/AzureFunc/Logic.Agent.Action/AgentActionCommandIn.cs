using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentActionCommandIn(
    int TelegramUpdateId,
    long TelegramUserId,
    long TelegramChatId,
    int TelegramMessageId,
    Guid ActionId,
    AgentActionType ActionType,
    AgentActionDecision Decision) : IChatCommandIn<Unit>
{
    public static string Type { get; } = "AgentAction";
}
