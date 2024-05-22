using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetShowDependency
{
    public static Dependency<IChatCommand<TimesheetShowCommandIn, Unit>> UseTimesheetShowCommand(
        this Dependency<ICrmTimesheetApi, TimesheetShowFlowOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IChatCommand<TimesheetShowCommandIn, Unit>>(CreateCommand);

        static TimesheetShowCommand CreateCommand(ICrmTimesheetApi timesheetApi, TimesheetShowFlowOption option)
        {
            ArgumentNullException.ThrowIfNull(timesheetApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(timesheetApi, option);
        }
    }
}