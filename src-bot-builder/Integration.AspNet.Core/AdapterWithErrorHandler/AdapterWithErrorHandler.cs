using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace GGroupp.Infra.Bot.Builder;

internal sealed class AdapterWithErrorHandler : BotFrameworkHttpAdapter
{
    static AdapterWithErrorHandler()
        =>
        HttpHelper.BotMessageSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

    public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger)
        : base(configuration, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "[OnTurnError] unhandled error : {0}", exception.Message);
            await turnContext.SendActivityAsync("Что-то пошло не так...");
        };
    }
}