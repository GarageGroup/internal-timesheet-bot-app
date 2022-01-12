using System;
using System.Threading;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

partial class BotDependency
{
    private static readonly Lazy<Dependency<IFunc<UserAuthorizeConfiguration>>> configurationProviderDependency
        =
        new(CreateConfigurationProviderDependency, LazyThreadSafetyMode.ExecutionAndPublication);

    internal static IFunc<UserAuthorizeConfiguration> GetUserAuthorizeConfigurationProvider(IBotContext botContext)
        =>
        configurationProviderDependency.Value.Resolve(botContext.ServiceProvider);

    private static Dependency<IFunc<UserAuthorizeConfiguration>> CreateConfigurationProviderDependency()
        =>
        Dependency.From<IFunc<UserAuthorizeConfiguration>>(
            sp => sp.GetRequiredService<IConfiguration>().Get<UserAuthorizeConfigurationJson>());
}