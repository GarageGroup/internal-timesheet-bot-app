using System;
using GGroupp.Infra.Bot.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

partial class BotHostBuilderExtensions
{
    public static IHostBuilder ConfigureBotWebHostDefaults(
        this IHostBuilder hostBuilder,
        Func<IBotBuilder, IBotBuilder> configureBot)
        =>
        InnerConfigureBotWebHostDefaults(
            hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder)),
            configureBot ?? throw new ArgumentNullException(nameof(configureBot)));

    private static IHostBuilder InnerConfigureBotWebHostDefaults(
        IHostBuilder builder,
        Func<IBotBuilder, IBotBuilder> configureBot)
        =>
        builder.ConfigureWebHostDefaults(
            b => b.Configure(app => app.Configure(configureBot)));

    private static void Configure(this IApplicationBuilder app, Func<IBotBuilder, IBotBuilder> configureBot)
        =>
        app
        .UseWebSockets()
        .UseAuthorization(
            _ => new())
        .UseBot(
            sp => sp.ResolveBot(configureBot));

    private static IBot ResolveBot(this IServiceProvider serviceProvider, Func<IBotBuilder, IBotBuilder> configureBot)
        =>
        BotBuilder.InternalCreate(
            serviceProvider,
            serviceProvider.GetRequiredService<ConversationState>(),
            serviceProvider.GetRequiredService<UserState>(),
            serviceProvider.GetRequiredService<ILoggerFactory>())
        .InnerPipe(
            configureBot)
        .Build();
}