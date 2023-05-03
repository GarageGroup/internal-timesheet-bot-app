using System;

namespace GGroupp.Internal.Timesheet;

public sealed record class TimesheetDateStepOption
{
    public TimesheetDateStepOption(string propertyDisplayName, short rowsCount)
    {
        PropertyDisplayName = propertyDisplayName.OrEmpty();
        RowsCount = rowsCount;
    }

    public string PropertyDisplayName { get; }

    public short RowsCount { get; }

    public bool SkipStep { get; init; }
}