using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class DateTimesheetEditOption
{
    public DateTimesheetEditOption(int allowedIntervalInDays, string urlWebApp)
    {
        AllowedIntervalInDays = allowedIntervalInDays;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int AllowedIntervalInDays { get; }

    public string UrlWebApp { get; }
}