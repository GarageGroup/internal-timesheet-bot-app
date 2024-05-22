namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetShowFlowOption
{
    public TimesheetShowFlowOption(int limitationDayOfMonth)
        =>
        LimitationDayOfMonth = limitationDayOfMonth;

    public int LimitationDayOfMonth { get; }
}