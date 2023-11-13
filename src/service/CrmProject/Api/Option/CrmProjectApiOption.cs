namespace GarageGroup.Internal.Timesheet;

public readonly record struct CrmProjectApiOption
{
    public int? LastProjectItemsCount { get; init; }

    public int? LastProjectDaysCount { get; init; }
}