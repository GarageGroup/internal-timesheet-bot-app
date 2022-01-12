using System;
namespace GGroupp.Internal.Timesheet;

public sealed record class UserLogOutOption
{
    public UserLogOutOption(string commandName)
        =>
        CommandName = commandName ?? "logout";

    public string CommandName { get; }
}

