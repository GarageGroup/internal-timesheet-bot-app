using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentActionDecisionOut(Guid ActionId, AgentActionDecision Decision);
