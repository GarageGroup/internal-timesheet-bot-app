using System;
using GarageGroup.Infra.Telegram.Bot;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentMessageCommandIn(
    int TelegramUpdateId,
    long TelegramUserId,
    long TelegramChatId,
    string Text,
    string VoiceFileId,
    string VoiceMimeType,
    long VoiceFileSize,
    string Locale) : IChatCommandIn<Unit>
{
    public static string Type { get; } = "AgentMessage";
}
