#nullable enable

using System;

namespace GGroupp.Internal.Timesheet.Bot
{
    internal sealed record TimesheetGetCommand
    {
        public Guid UserId { get; init; }

        public DateTimeOffset DateFrom { get; init; }

        public DateTimeOffset DateTo { get; init; }

        public override string ToString()
            =>
            FormattableString.Invariant($"Get timesheets for {UserId} from {DateFrom:dd.MM.yyyy} to {DateTo:dd.MM.yyyy}.");
    }
}