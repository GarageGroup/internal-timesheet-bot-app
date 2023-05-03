using System;
using GGroupp.Infra;
using GGroupp.Infra.Bot.Builder;
using Microsoft.Bot.Builder;
using PrimeFuncPack;

namespace GGroupp.Internal.Timesheet;

partial class Application
{
    [ServiceBusBotFunction("HandleServiceBusBotMessage", "sbq-gtimesheet-bot-message", "BotServiceBusConnection")]
    internal static Dependency<IBot> UseBot()
        =>
        Dependency.From(ResolveBot);

    private static IBot ResolveBot(this IServiceProvider serviceProvider)
        =>
        BotBuilder.Resolve(serviceProvider)
        .UseLogoutFlow()
        .UseBotStopFlow()
        .UseAuthorizationFlow()
        .UseBotInfoFlow()
        .UseTimesheetCreateFlow()
        .UseDateTimesheetGetFlow()
        .UseBotMenuFlow()
        .Build();
}