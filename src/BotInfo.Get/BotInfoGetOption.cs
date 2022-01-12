using System;
namespace GGroupp.Internal.Timesheet;

public sealed record class BotInfoGetOption
{
    public BotInfoGetOption(string commandName, string helloText)
    {
        CommandName = commandName ?? "info";
        HelloText = helloText ?? string.Empty;
    }

    public string CommandName { get; }

    public string HelloText { get; }
}

