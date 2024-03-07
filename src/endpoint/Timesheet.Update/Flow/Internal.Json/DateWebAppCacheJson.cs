using System.Collections.Generic;

namespace GarageGroup.Internal.Timesheet;

internal sealed record DateWebAppCacheJson(
    string? ActivityId,
    UpdateStatus Status = UpdateStatus.EditTimesheet,
    List<ProjectCacheJson>? Projects = default,
    ProjectCacheJson? EditedProject = null,
    UpdateTimesheetJson? EditTimesheetState = null);