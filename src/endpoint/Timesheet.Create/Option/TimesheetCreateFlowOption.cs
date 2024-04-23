using System;

namespace GarageGroup.Internal.Timesheet;

public sealed record class TimesheetCreateFlowOption
{
    public TimesheetCreateFlowOption(int limitationDay, string urlWebApp)
    {
        LimitationDay = limitationDay;
        UrlWebApp = urlWebApp.OrEmpty();
    }

    public int LimitationDay { get; }

    public string UrlWebApp { get; }
}