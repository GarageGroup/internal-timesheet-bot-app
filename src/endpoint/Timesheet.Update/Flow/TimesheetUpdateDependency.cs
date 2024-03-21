using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

public static class TimesheetUpdateDependency
{
    public static IBotBuilder MapTimesheetUpdateFlow(
        this Dependency<ICrmProjectApi, ICrmTimesheetApi> dependency, IBotBuilder botBuilder, string commandName)
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
                context.ServiceProvider.GetRequiredService<IConfiguration>().ResolveOptions(),
                cancellationToken);
    }

    private static UpdateTimesheetOptions ResolveOptions(this IConfiguration configuration)
        =>
        new ()
        {
            TimesheetInterval = TimeSpan.Parse(configuration.GetRequiredString("DeleteTimesheetOptions:TimesheetInterval")),
            UrlWebApp = configuration.GetRequiredString("DeleteTimesheetOptions:UrlWebApp")
        };

    private static string GetRequiredString(this IConfiguration configuration, string nameConfiguration)
        =>
        configuration[nameConfiguration] ?? throw new InvalidOperationException($"{nameConfiguration} is missing");
}