using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace GarageGroup.Internal.Timesheet;

public static class HealthCheckFunction
{
    [Function("HealthCheck")]
    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "application/json");
        response.WriteString("{\"status\": \"Healthy\"}");

        return response;
    }
}