using Flow.FlowStep;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

public static class DateTimesheetDeleteDependency
{
    public static IBotBuilder MapDateTimesheetDeleteFlow(
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
                context.ServiceProvider.GetRequiredService<IConfiguration>().ResolveOptions(),
                cancellationToken);
    }

    private static DeleteTimesheetOptions ResolveOptions(this IConfiguration configuration)
        =>
        new ()
        {
            TimesheetInterval = TimeSpan.Parse(configuration["DeleteTimesheetOptions:TimesheetInterval"] 
                ?? throw new InvalidOperationException("TimesheetInterval is missing")),
            UrlWebApp = configuration["DeleteTimesheetOptions:UrlWebApp"] 
                ?? throw new InvalidOperationException("UrlWebApp is missing")
        };
}