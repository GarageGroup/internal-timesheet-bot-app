using System;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;
using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;
using IUserAuthorizeConfigurationProvider = IFunc<UserAuthorizeConfiguration>;
using IUserAuthorizeMiddleware = IAsyncValueFunc<ITurnContext, TurnState>;

internal sealed partial class UserAuthorizeMiddleware : IUserAuthorizeMiddleware
{
    private readonly IAzureUserGetFunc azureUserGetFunc;
    private readonly IDataverseUserGetFunc dataverseUserGetFunc;
    private readonly IUserStateProvider userStateProvider;
    private readonly IUserAuthorizeConfigurationProvider userAuthorizeConfigurationProvider;
    private readonly ILogger logger;

    internal UserAuthorizeMiddleware(
        IAzureUserGetFunc azureUserGetFunc,
        IDataverseUserGetFunc dataverseUserGetFunc,
        IUserStateProvider userStateProvider,
        ILogger<UserAuthorizeMiddleware> logger,
        IUserAuthorizeConfigurationProvider userAuthorizeConfigurationProvider)
    {
        this.azureUserGetFunc = azureUserGetFunc;
        this.dataverseUserGetFunc = dataverseUserGetFunc;
        this.userStateProvider = userStateProvider;
        this.logger = logger;
        this.userAuthorizeConfigurationProvider = userAuthorizeConfigurationProvider;
    }

    private OAuthPromptSettings GetOAuthSettings(string text)
    {
        var configuration = userAuthorizeConfigurationProvider.Invoke();

        return new()
        {
            ConnectionName = configuration.OAuthConnectionName,
            Text = text,
            Title = "Вход",
            Timeout = configuration.OAuthTimeoutMilliseconds
        };
    }

    private static FlowStateJson CreateFlowState(OAuthPromptSettings settings)
    {
        var now = DateTimeOffset.Now;
        return new()
        {
            ExpirationDate = settings.Timeout.HasValue ? now.AddMilliseconds(settings.Timeout.Value) : now,
            Settings = settings
        };
    }
}