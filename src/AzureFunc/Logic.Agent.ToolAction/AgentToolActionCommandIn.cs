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
    // The query of the card widget that has sent the decision; null when an inline button was pressed
    public string? WidgetQueryId { get; init; }

    // The values the user has changed in the card widget
    public AgentActionEdit? Edit { get; init; }

    public static string Type { get; } = "AgentToolAction";
}
