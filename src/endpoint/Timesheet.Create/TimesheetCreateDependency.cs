using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetCreateDependency
{
    public static IBotBuilder MapTimesheetCreateFlow(
        this Dependency<ICrmProjectApi, ICrmTimesheetApi, TimesheetEditOption> dependency, IBotBuilder botBuilder, string commandName)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(botBuilder);

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext context, CancellationToken cancellationToken)
            =>
            context.RunAsync(
                commandName: commandName,
                crmProjectApi: dependency.ResolveFirst(context.ServiceProvider),
                crmTimesheetApi: dependency.ResolveSecond(context.ServiceProvider),
                option: dependency.ResolveThird(context.ServiceProvider),
                cancellationToken: cancellationToken);
    }
}