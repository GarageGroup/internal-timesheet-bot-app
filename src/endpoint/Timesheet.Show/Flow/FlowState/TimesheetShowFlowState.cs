using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetShowFlowState
{
    public TimesheetShowFlowState(int limitationDayOfMonth, Guid userId)
    {
        LimitationDayOfMonth = limitationDayOfMonth;
        UserId = userId;
    }

    public int LimitationDayOfMonth { get; }

    public Guid UserId { get; }

    public DateOnly? Date { get; init; }

    public string? MessageText { get; init; }

    public FlatArray<TimesheetJson> Timesheets { get; init; }
}