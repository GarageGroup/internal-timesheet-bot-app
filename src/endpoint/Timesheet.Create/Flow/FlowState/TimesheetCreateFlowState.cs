using System;

namespace GarageGroup.Internal.Timesheet;

internal sealed record class TimesheetCreateFlowState
{
    public TimesheetCreateFlowState(int limitationDayOfMonth, Guid userId)
    {
        LimitationDayOfMonth = limitationDayOfMonth;
        UserId = userId;
    }

    public int LimitationDayOfMonth { get; }

    public Guid UserId { get; }

    public Guid? TimesheetId { get; set; }

    public TimesheetProjectState? Project { get; init; }

    public DateOnly? Date { get; init; }

    public bool ShowSelectedDate { get; init; }

    public decimal? Duration { get; init; }

    public FlatArray<string> DescriptionTags { get; init; }

    public TimesheetDescriptionState? Description { get; init; }

    public bool WithoutConfirmation { get; set; }
}