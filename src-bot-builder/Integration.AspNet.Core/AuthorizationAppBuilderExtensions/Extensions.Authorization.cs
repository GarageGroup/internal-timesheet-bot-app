using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

partial class AuthorizationAppBuilderExtensions
{
    public static IApplicationBuilder UseAuthorization(
        this IApplicationBuilder appBuilder,
        Func<IServiceProvider, AuthorizationOptions> authorizationOptionsResolver)
        =>
        InnerUseAuthorization(
            appBuilder ?? throw new ArgumentNullException(nameof(appBuilder)),
            authorizationOptionsResolver ?? throw new ArgumentNullException(nameof(authorizationOptionsResolver)));

    private static IApplicationBuilder InnerUseAuthorization(
        IApplicationBuilder appBuilder,
        Func<IServiceProvider, AuthorizationOptions> authorizationOptionsResolver)
        =>
        appBuilder.UseAuthorization(
            sp => new DefaultAuthorizationPolicyProvider(Options.Create(authorizationOptionsResolver.Invoke(sp))));
}