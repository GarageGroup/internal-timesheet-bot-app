using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

public static class AgentToolLoaderTest
{
    private const string Specification =
        """
        {
          "openapi": "3.0.1",
          "info": { "title": "Timesheet API", "version": "1.0" },
          "servers": [ { "url": "https://untrusted.example/" } ],
          "paths": {
            "/timesheets": {
              "get": {
                "operationId": "GetTimesheets",
                "summary": "Returns time entries of the current user",
                "parameters": [
                  { "name": "dateFrom", "in": "query", "required": true, "schema": { "type": "string" } }
                ],
                "responses": { "200": { "description": "OK" } }
              },
              "post": {
                "operationId": "CreateTimesheet",
                "summary": "Creates a time entry",
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "properties": { "duration": { "type": "number" } }
                      }
                    }
                  }
                },
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private static readonly AgentUserContext User = new(TelegramUserId: 15, TelegramChatId: 27);

    [Fact]
    public static async Task LoadAsync_OpenApiTool_ExpectFunctionCallsApiWithUserTokenAndConfiguredBaseAddress()
    {
        var handler = CreateHandler();
        var tokenProvider = new FakeAgentUserTokenProvider("user-token");
        var loader = CreateLoader(handler, tokenProvider, CreateTool());

        await using var toolSet = await loader.LoadAsync(User, TestContext.Current.CancellationToken);

        var plugin = Assert.Single(toolSet.Plugins);
        Assert.Equal("Timesheet", plugin.Name);
        Assert.Equal(["CreateTimesheet", "GetTimesheets"], plugin.Select(static function => function.Name).Order());

        var result = await plugin["GetTimesheets"].InvokeAsync(
            new Kernel(),
            new() { ["dateFrom"] = "2026-11-23" },
            TestContext.Current.CancellationToken);

        Assert.Contains("Call with Albert", result.ToString());
        Assert.Equal([(User, "api://timesheet")], tokenProvider.Requests);

        var specificationRequest = handler.Requests[0];
        Assert.Equal("https://apim.example/swagger/swagger.json", specificationRequest.Uri.AbsoluteUri);
        Assert.Equal("subscription-key", specificationRequest.SubscriptionKey);
        Assert.Null(specificationRequest.Authorization);

        var apiRequest = handler.Requests[1];
        Assert.Equal("GET", apiRequest.Method, ignoreCase: true);
        Assert.Equal("https://apim.example/timesheet/timesheets?dateFrom=2026-11-23", apiRequest.Uri.AbsoluteUri);
        Assert.Equal("Bearer user-token", apiRequest.Authorization);
        Assert.Equal("subscription-key", apiRequest.SubscriptionKey);
    }

    [Fact]
    public static async Task LoadAsync_AllowedOperationsAreSpecified_ExpectOnlyAllowedFunctions()
    {
        var loader = CreateLoader(
            CreateHandler(),
            new FakeAgentUserTokenProvider("user-token"),
            CreateTool() with { AllowedOperations = ["gettimesheets"] });

        await using var toolSet = await loader.LoadAsync(User, TestContext.Current.CancellationToken);

        Assert.Equal(["GetTimesheets"], Assert.Single(toolSet.Plugins).Select(static function => function.Name));
    }

    [Fact]
    public static async Task LoadAsync_UserHasNoToken_ExpectNotSignedInExceptionAndNoApiCalls()
    {
        var handler = CreateHandler();
        var loader = CreateLoader(handler, new FakeAgentUserTokenProvider(accessToken: null), CreateTool());

        var exception = await Assert.ThrowsAsync<AgentUserNotSignedInException>(
            async () => await loader.LoadAsync(User, TestContext.Current.CancellationToken));

        Assert.Equal("api://timesheet", exception.Audience);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public static async Task LoadAsync_AudienceIsNotSpecified_ExpectNoTokenIsRequestedOrSent()
    {
        var handler = CreateHandler();
        var tokenProvider = new FakeAgentUserTokenProvider("user-token");
        var loader = CreateLoader(handler, tokenProvider, CreateTool() with { Audience = null });

        await using var toolSet = await loader.LoadAsync(User, TestContext.Current.CancellationToken);

        _ = await toolSet.Plugins[0]["GetTimesheets"].InvokeAsync(
            new Kernel(),
            new() { ["dateFrom"] = "2026-11-23" },
            TestContext.Current.CancellationToken);

        Assert.Empty(tokenProvider.Requests);
        Assert.Null(handler.Requests[^1].Authorization);
    }

    [Fact]
    public static async Task LoadAsync_CalledTwice_ExpectSpecificationIsLoadedOnce()
    {
        var handler = CreateHandler();
        var loader = CreateLoader(handler, new FakeAgentUserTokenProvider("user-token"), CreateTool());

        await using var first = await loader.LoadAsync(User, TestContext.Current.CancellationToken);
        await using var second = await loader.LoadAsync(User, TestContext.Current.CancellationToken);

        _ = Assert.Single(handler.Requests);
    }

    private static AgentToolLoader CreateLoader(
        FakeHttpMessageHandler handler, FakeAgentUserTokenProvider tokenProvider, AgentToolOption tool)
        =>
        new([tool], handler, tokenProvider, NullLoggerFactory.Instance);

    private static AgentToolOption CreateTool()
        =>
        new("Timesheet", AgentToolType.OpenApi, new("https://apim.example/swagger/swagger.json"))
        {
            BaseAddress = new("https://apim.example/timesheet/"),
            Audience = "api://timesheet",
            SubscriptionKey = "subscription-key"
        };

    private static FakeHttpMessageHandler CreateHandler()
        =>
        new(
            static request => request.RequestUri!.AbsolutePath.EndsWith("swagger.json", StringComparison.Ordinal)
                ? FakeHttpMessageHandler.Json(Specification)
                : request.RequestUri.Host is "apim.example"
                    ? FakeHttpMessageHandler.Json("""[{"project":"Presales (MS)","description":"Call with Albert"}]""")
                    : new HttpResponseMessage(HttpStatusCode.BadGateway));
}
