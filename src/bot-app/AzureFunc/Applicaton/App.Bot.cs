using System;
using GarageGroup.Infra;
using GarageGroup.Infra.Bot.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Bot.Builder;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

partial class Application
{
    [HttpBotFunction("HandleHttpBotMessage", AuthLevel = AuthorizationLevel.Function)]
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
        .Build(true);
}