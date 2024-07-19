using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class WelcomeDependency
{
    internal static Dependency<IChatCommand<WelcomeCommandIn, Unit>> UseWelcomeCommand(
        this Dependency<WelcomeOption> dependency)
    {
        return dependency.Map<IChatCommand<WelcomeCommandIn, Unit>>(CreateCommand);

        static WelcomeCommand CreateCommand(WelcomeOption option)
            =>
            new(option);
    }
}