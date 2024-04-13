using System;
using System.Threading;
using System.Threading.Tasks;
using GarageGroup.Infra.Bot.Builder;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class DateTimesheetGetDependency
{
    public static IBotBuilder MapDateTimesheetGetFlow(
        this Dependency<ICrmTimesheetApi, DateTimesheetEditOption> dependency, IBotBuilder botBuilder, string commandName)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        ArgumentNullException.ThrowIfNull(botBuilder);

        return botBuilder.Use(InnerInvokeAsync);

        ValueTask<Unit> InnerInvokeAsync(IBotContext context, CancellationToken cancellationToken)
            =>
            context.RunAsync(
                commandName,
                dependency.ResolveFirst(context.ServiceProvider),
                dependency.ResolveSecond(context.ServiceProvider),
                cancellationToken);
    }
}