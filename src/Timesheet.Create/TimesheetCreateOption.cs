using System;
namespace GGroupp.Internal.Timesheet;

public sealed record class TimesheetCreateOption
{
    public TimesheetCreateOption(string commandName)
        =>
        CommandName = commandName ?? "newtimesheet";

    public string CommandName { get; }
}

