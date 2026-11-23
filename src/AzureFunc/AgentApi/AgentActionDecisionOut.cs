using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentActionDecisionOut(Guid ActionId, AgentActionDecision Decision)
{
    public DateOnly Date { get; init; }

    public bool TimesheetsLoaded { get; init; }

    public AgentActionTimesheetOut[] Timesheets { get; init; } = [];
}
