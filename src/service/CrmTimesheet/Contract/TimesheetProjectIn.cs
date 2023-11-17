using System;
using System.Diagnostics.CodeAnalysis;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetProjectIn
{
    public TimesheetProjectIn(Guid id, TimesheetProjectType type, [AllowNull] string displayName)
    {
        Id = id;
        Type = type;
        DisplayName = displayName.OrNullIfEmpty();
    }

    public Guid Id { get; }

    public TimesheetProjectType Type { get; }

    public string? DisplayName { get; }
}