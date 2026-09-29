using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentMessage(string Text, AgentPreparedAction? PreparedAction = null);

internal sealed record class AgentPreparedAction(
    Guid ActionId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    int ProjectType,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);
