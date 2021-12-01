using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

public static partial class AuthorizationAppBuilderExtensions
{
    private static AuthorizationMiddleware GetAuthorizationMiddleware(
        this HttpContext context,
        RequestDelegate next,
        Func<IServiceProvider, IAuthorizationPolicyProvider> authorizationPolicyProviderResolver)
        =>
        new(
            next,
            authorizationPolicyProviderResolver.Invoke(context.RequestServices));
}