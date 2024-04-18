using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetCreateFlowOption
{
    public TimesheetCreateFlowOption(int allowedIntervalInDays, string urlWebApp)
    {
        AllowedIntervalInDays = allowedIntervalInDays;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int AllowedIntervalInDays { get; }

    public string UrlWebApp { get; }
}