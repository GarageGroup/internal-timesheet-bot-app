using System;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra.Bot.Builder;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

public static class DateTimesheetGetDependency
{
    public static IBotBuilder MapDateTimesheetGetFlow<TTimesheetApi>(
        this Dependency<TTimesheetApi> dependency, IBotBuilder botBuilder, string commandName)
        where TTimesheetApi : ITimesheetSetGetSupplier
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