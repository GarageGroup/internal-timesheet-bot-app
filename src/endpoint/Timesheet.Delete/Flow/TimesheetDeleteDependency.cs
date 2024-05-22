using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetDeleteDependency
{
    public static Dependency<IChatCommand<TimesheetDeleteCommandIn, Unit>> UseTimesheetDeleteCommand(
        this Dependency<ICrmTimesheetApi> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Map<IChatCommand<TimesheetDeleteCommandIn, Unit>>(CreateCommand);

        static TimesheetDeleteCommand CreateCommand(ICrmTimesheetApi timesheetApi)
        {
            ArgumentNullException.ThrowIfNull(timesheetApi);
            return new(timesheetApi);
        }
    }
}