using System;
namespace GGroupp.Internal.Timesheet;

public sealed record class BotConversationCancelOption
{
    public BotConversationCancelOption(string commandName, string successText)
    {
        CommandName = commandName ?? "cancel";
        SuccessText = successText ?? string.Empty;
    }

    public string CommandName { get; }

    public string SuccessText { get; }
}

