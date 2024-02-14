using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetDeleteIn
{
    public TimesheetDeleteIn(Guid timesheetId)
    {
        TimesheetId = timesheetId;
    }
    
    public Guid TimesheetId { get; }
    
}