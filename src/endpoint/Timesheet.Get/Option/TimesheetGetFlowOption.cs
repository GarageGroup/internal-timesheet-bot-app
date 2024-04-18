using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetGetFlowOption
{
    public TimesheetGetFlowOption(int allowedIntervalInDays, string urlWebApp)
    {
        AllowedIntervalInDays = allowedIntervalInDays;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int AllowedIntervalInDays { get; }

    public string UrlWebApp { get; }
}