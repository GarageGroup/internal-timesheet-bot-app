using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentMessage(
    string Text,
    AgentPreparedCreateAction? PreparedCreateAction = null,
    AgentPreparedDeleteAction? PreparedDeleteAction = null,
    AgentPreparedUpdateAction? PreparedUpdateAction = null);

internal sealed record class AgentPreparedCreateAction(
    Guid ActionId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    int ProjectType,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);

internal sealed record class AgentPreparedDeleteAction(
    Guid ActionId,
    Guid TimesheetId,
    DateOnly Date,
    string ProjectName,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);

internal sealed record class AgentPreparedUpdateAction(
    Guid ActionId,
    Guid TimesheetId,
    DateOnly Date,
    Guid ProjectId,
    string ProjectName,
    int ProjectType,
    decimal Duration,
    string Description,
    DateTimeOffset ExpiresAt);
