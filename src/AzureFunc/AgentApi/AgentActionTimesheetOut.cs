using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class AgentActionTimesheetOut(
    Guid Id,
    string ProjectName,
    string ProjectType,
    decimal Duration,
    string Description,
    bool IsActive);
