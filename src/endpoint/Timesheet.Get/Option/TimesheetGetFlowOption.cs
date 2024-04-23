using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetGetFlowOption
{
    public TimesheetGetFlowOption(int limitationDay, string urlWebApp)
    {
        LimitationDay = limitationDay;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int LimitationDay { get; }

    public string UrlWebApp { get; }
}