using GarageGroup.Infra.Bot.Builder;
using PrimeFuncPack;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetDeleteDependency
{
    public static IBotBuilder MapTimesheetDeleteFlow(
        this Dependency<ICrmTimesheetApi> dependency, IBotBuilder botBuilder, string commandName)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(botBuilder);

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext context, CancellationToken cancellationToken)
            =>
            context.RunAsync(
                commandName,
                dependency.Resolve(context.ServiceProvider),
                cancellationToken);
    }
}