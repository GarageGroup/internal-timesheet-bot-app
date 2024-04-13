using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetEditOption
{
    public TimesheetEditOption(int allowedIntervalInDays, string urlWebApp)
    {
        AllowedIntervalInDays = allowedIntervalInDays;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int AllowedIntervalInDays { get; }

    public string UrlWebApp { get; }
}