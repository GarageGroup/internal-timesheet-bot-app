using System;
using GGroupp.Infra.Bot.Builder;
using GGroupp.Platform;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace GGroupp.Internal.Timesheet;

using IAzureUserGetFunc = IAsyncValueFunc<AzureUserMeGetIn, Result<AzureUserGetOut, Failure<AzureUserGetFailureCode>>>;
using IDataverseUserGetFunc = IAsyncValueFunc<DataverseUserGetIn, Result<DataverseUserGetOut, Failure<DataverseUserGetFailureCode>>>;
using IUserAuthorizeConfigurationProvider = IFunc<UserAuthorizeConfiguration>;
using IUserAuthorizeMiddleware = IAsyncValueFunc<ITurnContext, Unit>;

internal sealed partial class UserAuthorizeMiddleware : IUserAuthorizeMiddleware
{
    private readonly IAzureUserGetFunc azureUserGetFunc;
    private readonly IDataverseUserGetFunc dataverseUserGetFunc;
    private readonly IBotUserProvider botUserProvider;
    private readonly IBotFlow botFlow;
    private readonly IStatePropertyAccessor<Activity?> sourceActivityAccessor;
    private readonly string connectionName;
    private readonly ILogger logger;

    internal UserAuthorizeMiddleware(
        IAzureUserGetFunc azureUserGetFunc,
        IDataverseUserGetFunc dataverseUserGetFunc,
        IBotContext botContext,
        UserState userState,
        IUserAuthorizeConfigurationProvider userAuthorizeConfigurationProvider)
    {
        this.azureUserGetFunc = azureUserGetFunc;
        this.dataverseUserGetFunc = dataverseUserGetFunc;
        botUserProvider = botContext.BotUserProvider;
        botFlow = botContext.BotFlow;
        sourceActivityAccessor = userState.CreateProperty<Activity?>("__authSourceActivity");
        logger = botContext.LoggerFactory.CreateLogger<UserAuthorizeMiddleware>();
        connectionName = userAuthorizeConfigurationProvider.Invoke().OAuthConnectionName;
    }

    private IOAuthFlowContext CreateFlowContext(ITurnContext turnContext)
        =>
        new OAuthFlowContextImpl(turnContext, logger);
}