using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentToolActionCommandIn(
    long TelegramUserId,
    long TelegramChatId,
    int TelegramMessageId,
    string ActionId,
    bool IsConfirmed,
    string Locale) : IChatCommandIn<Unit>
{
    public static string Type { get; } = "AgentToolAction";
}
