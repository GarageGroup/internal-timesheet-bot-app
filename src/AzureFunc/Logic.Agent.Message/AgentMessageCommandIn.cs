using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentMessageCommandIn(
    int TelegramUpdateId,
    long TelegramUserId,
    long TelegramChatId,
    string Text,
    string? Locale) : IChatCommandIn<Unit>
{
    public static string Type { get; } = "AgentMessage";
}
