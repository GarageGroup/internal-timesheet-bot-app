using System;
using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class SignOutDependency
{
    internal static Dependency<IChatCommand<SignOutCommandIn, Unit>> UseSignOutCommand<TAuthorizationApi>(
        this Dependency<TAuthorizationApi> dependency)
        where TAuthorizationApi : IUserUnauthorizeSupplier
    {
        return dependency.Map<IChatCommand<SignOutCommandIn, Unit>>(CreateCommand);

        static SignOutCommand CreateCommand(TAuthorizationApi authorizationApi)
            =>
            new(authorizationApi);
    }
}