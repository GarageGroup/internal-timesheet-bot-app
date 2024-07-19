using GarageGroup.Infra.Telegram.Bot;
using PrimeFuncPack;

namespace GarageGroup.Internal.Timesheet;

internal static class CultureDependency
{
    internal static Dependency<IChatMiddleware> UseCultureMiddleware<TAuthorizationApi>(
        this Dependency<TAuthorizationApi> dependency)
        where TAuthorizationApi : IChatUserGetSupplier
    {
        return dependency.Map<IChatMiddleware>(CreateMiddleware);

        static CultureMiddleware CreateMiddleware(TAuthorizationApi authorizationApi)
            =>
            new(authorizationApi);
    }
}