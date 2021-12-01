using System;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Builder;

partial class AuthorizationAppBuilderExtensions
{
    public static IApplicationBuilder UseAuthorization(
        this IApplicationBuilder appBuilder,
        Func<IServiceProvider, IAuthorizationPolicyProvider> authorizationPolicyProviderResolver)
        =>
        InnerUseAuthorization(
            appBuilder ?? throw new ArgumentNullException(nameof(appBuilder)),
            authorizationPolicyProviderResolver ?? throw new ArgumentNullException(nameof(authorizationPolicyProviderResolver)));

    private static IApplicationBuilder InnerUseAuthorization(
        IApplicationBuilder appBuilder,
        Func<IServiceProvider, IAuthorizationPolicyProvider> authorizationPolicyProviderResolver)
        =>
        appBuilder.Use(
            next => context => context.GetAuthorizationMiddleware(next, authorizationPolicyProviderResolver).Invoke(context));
}