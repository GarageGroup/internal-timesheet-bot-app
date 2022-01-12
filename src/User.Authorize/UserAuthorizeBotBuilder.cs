using System;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;
using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;
using IUserAuthorizeConfigurationProvider = IFunc<UserAuthorizeConfiguration>;

public static class UserAuthorizeBotBuilder
{
    public static IBotBuilder UseAuthorization(
        this IBotBuilder botBuilder,
        Func<IBotContext, IAzureUserGetFunc> azureUserGetFuncResolver,
        Func<IBotContext, IDataverseUserGetFunc> dataverseUserGetFuncResolver,
        Func<IBotContext, IUserAuthorizeConfigurationProvider> configurationProviderResolver)
        =>
        InnerUseAuthorization(
            botBuilder ?? throw new ArgumentNullException(nameof(botBuilder)),
            azureUserGetFuncResolver ?? throw new ArgumentNullException(nameof(azureUserGetFuncResolver)),
            dataverseUserGetFuncResolver ?? throw new ArgumentNullException(nameof(dataverseUserGetFuncResolver)),
            configurationProviderResolver ?? throw new ArgumentNullException(nameof(configurationProviderResolver)));

    private static IBotBuilder InnerUseAuthorization(
        IBotBuilder botBuilder,
        Func<IBotContext, IAzureUserGetFunc> azureUserGetFuncResolver,
        Func<IBotContext, IDataverseUserGetFunc> dataverseUserGetFuncResolver,
        Func<IBotContext, IUserAuthorizeConfigurationProvider> configurationProviderResolver)
        =>
        botBuilder.Use(
            (context, token) => new UserAuthorizeMiddleware(
                azureUserGetFunc: azureUserGetFuncResolver.Invoke(context),
                dataverseUserGetFunc: dataverseUserGetFuncResolver.Invoke(context),
                userState: context.UserState,
                botContext: context,
                userAuthorizeConfigurationProvider: configurationProviderResolver.Invoke(context))
            .InvokeAsync(context.TurnContext, token));
}