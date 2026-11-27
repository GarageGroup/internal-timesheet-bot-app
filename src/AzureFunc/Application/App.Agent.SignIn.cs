using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using GarageGroup.Infra.Telegram.Bot;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GarageGroup.Internal.Timesheet;

public static class AgentSignInCallbackFunction
{
    // Microsoft Entra ID posts the authorization code here (response_mode=form_post) through API Management
    [Function("AgentSignInCallback")]
    public static async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "oauth2/callback")] HttpRequestData request,
        [DurableClient] DurableTaskClient durableClient,
        FunctionContext context)
    {
        var serviceProvider = context.InstanceServices;
        var signInService = serviceProvider.GetRequiredService<AgentSignInRuntime>().Service;

        if (signInService is null)
        {
            return request.CreateResponse(HttpStatusCode.NotFound);
        }

        var body = await request.ReadAsStringAsync().ConfigureAwait(false);
        var values = HttpUtility.ParseQueryString(body ?? string.Empty);

        var form = new Dictionary<string, string>();
        foreach (var key in values.AllKeys)
        {
            if (key is not null)
            {
                form[key] = values[key] ?? string.Empty;
            }
        }

        var handler = new AgentSignInCallbackHandler(
            signInService,
            new AgentSignInNotifier(
                botApi: serviceProvider.GetRequiredService<BotProvider>().BotApi,
                signalHandler: Application.UseBotSignal(durableClient).Resolve(serviceProvider)),
            serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AgentSignInCallback"));

        var result = await handler.HandleAsync(form, context.CancellationToken).ConfigureAwait(false);

        var response = request.CreateResponse(result.StatusCode);
        response.Headers.Add("Content-Type", "text/html; charset=utf-8");
        response.Headers.Add("Cache-Control", "no-store");
        response.Headers.Add("Referrer-Policy", "no-referrer");
        response.Headers.Add("X-Frame-Options", "DENY");
        response.Headers.Add("Content-Security-Policy", "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'");

        await response.WriteStringAsync(result.Html, context.CancellationToken).ConfigureAwait(false);
        return response;
    }
}
