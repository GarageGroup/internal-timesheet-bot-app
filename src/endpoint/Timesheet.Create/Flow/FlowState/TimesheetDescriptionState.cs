using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetDescriptionState
{
    public TimesheetDescriptionState([AllowNull] string value)
        =>
        Value = value.OrNullIfWhiteSpace();

    public string? Value { get; }
}