namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetCreateFlowOption
{
    public TimesheetCreateFlowOption(int limitationDayOfMonth)
        =>
        LimitationDayOfMonth = limitationDayOfMonth;

    public int LimitationDayOfMonth { get; }
}