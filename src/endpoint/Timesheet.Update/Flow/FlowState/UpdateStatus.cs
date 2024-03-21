using Newtonsoft.Json;
using System;

namespace GarageGroup.Internal.Timesheet;

internal enum UpdateStatus
{
    EditTimesheet,

    EditingProject,

    ProjectIsEdited
}