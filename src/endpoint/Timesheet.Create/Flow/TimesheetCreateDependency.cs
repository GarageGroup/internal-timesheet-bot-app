using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetCreateDependency
{
    public static Dependency<IChatCommand<TimesheetCreateCommandIn, Unit>> UseTimesheetCreateCommand(
        this Dependency<ICrmTimesheetApi, ICrmProjectApi, TimesheetCreateFlowOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Fold<IChatCommand<TimesheetCreateCommandIn, Unit>>(CreateCommand);

        static TimesheetCreateCommand CreateCommand(ICrmTimesheetApi timesheetApi, ICrmProjectApi projectApi, TimesheetCreateFlowOption option)
        {
            ArgumentNullException.ThrowIfNull(timesheetApi);
            ArgumentNullException.ThrowIfNull(projectApi);
            ArgumentNullException.ThrowIfNull(option);

            return new(timesheetApi, projectApi, option);
        }
    }
}