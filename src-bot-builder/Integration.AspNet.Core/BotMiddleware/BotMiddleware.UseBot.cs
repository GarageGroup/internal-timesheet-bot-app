using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;

namespace Microsoft.AspNetCore.Builder;

partial class BotMiddleware
{
    public static IApplicationBuilder UseBot(this IApplicationBuilder appBuilder, Func<IServiceProvider, IBot> botResolver)
        =>
        InternalUseBot(
            appBuilder ?? throw new ArgumentNullException(nameof(appBuilder)),
            botResolver ?? throw new ArgumentNullException(nameof(botResolver)));

    internal static IApplicationBuilder InternalUseBot(this IApplicationBuilder appBuilder, Func<IServiceProvider, IBot> botResolver)
        =>
        appBuilder.Map(
            new PathString("/api/messages"),
            app => app.Use(_ => ctx => InvokeBotAsync(ctx, botResolver)));
}