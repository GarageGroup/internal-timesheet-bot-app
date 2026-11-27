using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Xunit;

namespace GarageGroup.Internal.Timesheet.Test;

// These tests run the real Semantic Kernel OpenAI connector against a fake model endpoint and a fake Timesheet API,
// so they cover the whole loop: the model requests tools, the bot executes or captures them
public static class AgentActionFlowTest
{
    private const string ProjectId = "839bb7ea-6e1f-4ba0-a5fb-a63769a20e65";

    private const string Specification =
        """
        {
          "openapi": "3.0.1",
          "info": { "title": "Timesheet API", "version": "1.0" },
          "paths": {
            "/searchProjects": {
              "post": {
                "operationId": "search-projects",
                "summary": "Search for projects",
                "requestBody": { "content": { "application/json": { "schema": {
                  "type": "object", "properties": { "searchText": { "type": "string" } } } } } },
                "responses": { "200": { "description": "OK" } }
              }
            },
            "/updateProfile": {
              "post": {
                "operationId": "update-profile",
                "summary": "Update profile",
                "requestBody": { "content": { "application/json": { "schema": {
                  "type": "object", "properties": { "languageCode": { "type": "string", "enum": ["en", "ru"] } } } } } },
                "responses": { "200": { "description": "OK" } }
              }
            },
            "/createTimesheet": {
              "post": {
                "operationId": "create-timesheet",
                "summary": "Creates a new timesheet entry",
                "requestBody": { "content": { "application/json": { "schema": {
                  "type": "object",
                  "properties": {
                    "date": { "type": "string", "format": "date" },
                    "project": { "type": "object", "properties": {
                      "id": { "type": "string", "format": "uuid" },
                      "type": { "type": "string", "enum": ["Opportunity", "Lead", "Incident", "Project"] } } },
                    "duration": { "type": "number" },
                    "description": { "type": "string" } } } } } },
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private const string CreateArguments =
        $$"""{"date":"2026-11-27","id":"{{ProjectId}}","type":"Project","duration":2,"description":"#Test"}""";

    private static readonly AgentUserContext User = new(TelegramUserId: 15, TelegramChatId: 27);

    [Fact]
    public static async Task SendAsync_ModelRequestsReadThenWriteTool_ExpectReadIsExecutedAndWriteIsOnlyPrepared()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-search-projects", """{"searchText":"E2E"}"""),
            ToolCall("Timesheet-create-timesheet", CreateArguments));

        var reply = await context.Service.SendAsync(new(User, "спиши 2 часа на E2E", "ru"), TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, reply.Text);
        Assert.NotNull(reply.Action);
        var call = Assert.Single(reply.Action.Calls);
        Assert.Equal("Timesheet", call.ToolName);
        Assert.Equal("create-timesheet", call.FunctionName);

        // The project name comes from the API response of the previous tool call, not from the model
        Assert.Equal(
            [
                new("date", "2026-11-27"),
                new("id", ProjectId, "E2E Role test"),
                new("type", "Project"),
                new("duration", "2"),
                new("description", "#Test")
            ],
            call.Arguments);

        // The model was not called again after the action had been captured
        Assert.Equal(2, context.Model.Requests.Count);

        // Only the read operation reached the API
        Assert.Equal(["/timesheet/searchProjects"], context.ApiPaths);
        Assert.Equal("""{"searchText":"E2E"}""", context.Api.Requests[^1].Body);

        Assert.Same(reply.Action, Assert.Single(context.Actions.Actions.Values));
        Assert.Contains("have not been executed", Assert.Single(context.History.Turns).AssistantText);
    }

    [Fact]
    public static async Task ConfirmActionAsync_PreparedAction_ExpectApiIsCalledOnceWithCapturedArguments()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-create-timesheet", CreateArguments),
            Text("<b>Списание создано</b>"));

        var prepared = await context.Service.SendAsync(new(User, "спиши 2 часа", "ru"), TestContext.Current.CancellationToken);
        var actionId = prepared.Action!.Id;

        var outcome = await context.Service.ConfirmActionAsync(User, actionId, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.Succeeded, outcome.Status);
        Assert.Equal("<b>Списание создано</b>", outcome.Reply?.Text);
        Assert.Null(outcome.Reply?.Action);

        Assert.Equal(["/timesheet/createTimesheet"], context.ApiPaths);
        Assert.Equal("Bearer user-token", context.Api.Requests[^1].Authorization);

        using var body = JsonDocument.Parse(context.Api.Requests[^1].Body);
        Assert.Equal("2026-11-27", body.RootElement.GetProperty("date").GetString());
        Assert.Equal(ProjectId, body.RootElement.GetProperty("project").GetProperty("id").GetString());
        Assert.Equal("Project", body.RootElement.GetProperty("project").GetProperty("type").GetString());
        Assert.Equal(2, body.RootElement.GetProperty("duration").GetDecimal());
        Assert.Equal("#Test", body.RootElement.GetProperty("description").GetString());

        // The model is told the result of the executed action
        Assert.Contains("has confirmed the prepared actions", context.Model.Requests[^1].Body);
        Assert.Contains("isSuccess", context.Model.Requests[^1].Body);

        // The action cannot be executed twice
        var second = await context.Service.ConfirmActionAsync(User, actionId, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.NotFound, second.Status);
        _ = Assert.Single(context.ApiPaths);
    }

    [Fact]
    public static async Task ConfirmActionAsync_ApiFails_ExpectFailedStatusAndModelIsToldAboutTheError()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-create-timesheet", CreateArguments),
            Text("Не удалось создать списание."));

        context.ApiStatusCode = System.Net.HttpStatusCode.BadRequest;

        var prepared = await context.Service.SendAsync(new(User, "спиши 2 часа", "ru"), TestContext.Current.CancellationToken);
        var outcome = await context.Service.ConfirmActionAsync(User, prepared.Action!.Id, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.Failed, outcome.Status);
        Assert.Equal("Не удалось создать списание.", outcome.Reply?.Text);
        Assert.Contains("NOT executed. Error:", context.Model.Requests[^1].Body);
    }

    [Fact]
    public static async Task SendAsync_ModelRequestsSeveralWriteToolsAtOnce_ExpectAllArePreparedAsOneAction()
    {
        var context = new FlowContext(
            ToolCalls(
                ("Timesheet-search-projects", """{"searchText":"E2E"}"""),
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-23")),
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-24")),
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-25"))));

        var reply = await context.Service.SendAsync(new(User, "спиши по 2 часа с пн по ср", "ru"), TestContext.Current.CancellationToken);

        Assert.NotNull(reply.Action);
        Assert.Equal(
            ["2026-11-23", "2026-11-24", "2026-11-25"],
            reply.Action.Calls.Select(static call => call.Arguments.Single(static argument => argument.Name is "date").Value));

        // One model request, one stored action, and only the read operation reached the API
        _ = Assert.Single(context.Model.Requests);
        Assert.Same(reply.Action, Assert.Single(context.Actions.Actions.Values));
        Assert.Equal(["/timesheet/searchProjects"], context.ApiPaths);
    }

    [Fact]
    public static async Task ConfirmActionAsync_OneOfSeveralCallsFails_ExpectTheRestAreExecutedAndModelIsToldEachResult()
    {
        var context = new FlowContext(
            ToolCalls(
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-23")),
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-24")),
                ("Timesheet-create-timesheet", CreateArgumentsFor("2026-11-25"))),
            Text("Выполнено 2 из 3."));

        context.FailingBodyText = "2026-11-24";

        var prepared = await context.Service.SendAsync(new(User, "спиши по 2 часа с пн по ср", "ru"), TestContext.Current.CancellationToken);
        var outcome = await context.Service.ConfirmActionAsync(User, prepared.Action!.Id, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.PartiallySucceeded, outcome.Status);
        Assert.Equal("Выполнено 2 из 3.", outcome.Reply?.Text);

        // Every call reached the API exactly once, the failed one included
        Assert.Equal(3, context.ApiPaths.Length);
        Assert.All(context.ApiPaths, static path => Assert.Equal("/timesheet/createTimesheet", path));

        using var modelRequest = JsonDocument.Parse(context.Model.Requests[^1].Body);
        var eventText = modelRequest.RootElement.GetProperty("messages").EnumerateArray().Last().GetProperty("content").ToString();

        Assert.Equal(2, eventText.Split("— EXECUTED.").Length - 1);
        Assert.Equal(1, eventText.Split("— NOT executed.").Length - 1);
        Assert.Contains("2 of 3 actions have been executed", eventText);

        // The whole action is processed: it cannot be confirmed again
        var second = await context.Service.ConfirmActionAsync(User, prepared.Action.Id, "ru", TestContext.Current.CancellationToken);
        Assert.Equal(AgentActionStatus.NotFound, second.Status);
        Assert.Equal(3, context.ApiPaths.Length);
    }

    [Fact]
    public static async Task SendAsync_ModelRequestsTooManyWriteTools_ExpectNothingIsPreparedAndModelExplains()
    {
        var calls = Enumerable.Range(0, AgentToolInvocationFilter.MaxCapturedCalls + 1)
            .Select(static index => ("Timesheet-create-timesheet", CreateArgumentsFor(new DateOnly(2026, 10, 1).AddDays(index).ToString("yyyy-MM-dd"))))
            .ToArray();

        var context = new FlowContext(ToolCalls(calls), Text("Слишком много действий за раз."));

        var reply = await context.Service.SendAsync(new(User, "спиши на весь месяц", "ru"), TestContext.Current.CancellationToken);

        Assert.Null(reply.Action);
        Assert.Equal("Слишком много действий за раз.", reply.Text);
        Assert.Empty(context.Actions.Actions);
        Assert.Empty(context.ApiPaths);
        Assert.Contains("NONE of them has been prepared", context.Model.Requests[^1].Body);
    }

    [Fact]
    public static async Task ConfirmActionAsync_ActionIsExpiredOrBelongsToAnotherUser_ExpectNotFoundAndNoApiCall()
    {
        var context = new FlowContext(ToolCall("Timesheet-create-timesheet", CreateArguments));
        var prepared = await context.Service.SendAsync(new(User, "спиши 2 часа", "ru"), TestContext.Current.CancellationToken);

        var foreign = await context.Service.ConfirmActionAsync(
            new(TelegramUserId: 99, TelegramChatId: 27), prepared.Action!.Id, "ru", TestContext.Current.CancellationToken);

        context.Time.Now = context.Time.Now.AddMinutes(10);
        var expired = await context.Service.ConfirmActionAsync(User, prepared.Action.Id, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.NotFound, foreign.Status);
        Assert.Equal(AgentActionStatus.NotFound, expired.Status);
        Assert.Empty(context.ApiPaths);
    }

    [Fact]
    public static async Task CancelActionAsync_PreparedAction_ExpectActionIsRemovedAndApiIsNotCalled()
    {
        var context = new FlowContext(ToolCall("Timesheet-create-timesheet", CreateArguments));
        var prepared = await context.Service.SendAsync(new(User, "спиши 2 часа", "ru"), TestContext.Current.CancellationToken);

        var outcome = await context.Service.CancelActionAsync(User, prepared.Action!.Id, TestContext.Current.CancellationToken);
        var confirmed = await context.Service.ConfirmActionAsync(User, prepared.Action.Id, "ru", TestContext.Current.CancellationToken);

        Assert.Equal(AgentActionStatus.Cancelled, outcome.Status);
        Assert.Equal(AgentActionStatus.NotFound, confirmed.Status);
        Assert.Empty(context.ApiPaths);
        Assert.Contains("has cancelled the prepared actions", context.History.Turns[^1].UserText);
    }

    [Theory]
    [InlineData("Timesheet.create-timesheet", "Timesheet", "create-timesheet", false)]
    [InlineData("timesheet.CREATE-timesheet", "Timesheet", "create-timesheet", false)]
    [InlineData("Timesheet.*", "Timesheet", "create-timesheet", false)]
    [InlineData("Timesheet.search-projects", "Timesheet", "create-timesheet", true)]
    [InlineData("Docs.*", "Timesheet", "create-timesheet", true)]
    [InlineData("", "Timesheet", "create-timesheet", true)]
    public static void RequiresConfirmation_ExpectConfirmationUnlessFunctionOrWholeToolIsListed(
        string listed, string toolName, string functionName, bool expected)
    {
        var noConfirmationFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (listed.Length > 0)
        {
            _ = noConfirmationFunctions.Add(listed);
        }

        Assert.Equal(expected, AgentToolInvocationFilter.RequiresConfirmation(noConfirmationFunctions, toolName, functionName));
    }

    [Fact]
    public static async Task SendAsync_OperationDoesNotRequireConfirmation_ExpectItIsExecutedImmediately()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-search-projects", """{"searchText":"E2E"}"""),
            Text("Найден проект <code>E2E Role test</code>"));

        var reply = await context.Service.SendAsync(new(User, "найди проект E2E", "ru"), TestContext.Current.CancellationToken);

        Assert.Equal("Найден проект <code>E2E Role test</code>", reply.Text);
        Assert.Null(reply.Action);
        Assert.Equal(["/timesheet/searchProjects"], context.ApiPaths);
        Assert.Empty(context.Actions.Actions);
    }

    [Fact]
    public static async Task SendAsync_ModelChangesUserLanguage_ExpectItIsExecutedImmediatelyAndSettingsChangeIsReported()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-update-profile", """{"languageCode":"ru"}"""),
            Text("Язык общения изменён на русский."));

        var reply = await context.Service.SendAsync(new(User, "поменяй на русский", "en"), TestContext.Current.CancellationToken);

        Assert.Equal("Язык общения изменён на русский.", reply.Text);
        Assert.Null(reply.Action);
        Assert.True(reply.UserSettingsChanged);

        Assert.Equal(["/timesheet/updateProfile"], context.ApiPaths);
        Assert.Equal("""{"languageCode":"ru"}""", context.Api.Requests[^1].Body);
    }

    [Fact]
    public static async Task SendAsync_ModelOnlyReadsData_ExpectSettingsChangeIsNotReported()
    {
        var context = new FlowContext(
            ToolCall("Timesheet-search-projects", """{"searchText":"E2E"}"""),
            Text("Найден проект."));

        var reply = await context.Service.SendAsync(new(User, "найди проект", "ru"), TestContext.Current.CancellationToken);

        Assert.False(reply.UserSettingsChanged);
    }

    private static string ToolCall(string name, string arguments)
        =>
        ToolCalls((name, arguments));

    private static string CreateArgumentsFor(string date)
        =>
        CreateArguments.Replace("2026-11-27", date, StringComparison.Ordinal);

    // One model answer with several tool calls
    private static string ToolCalls(params (string Name, string Arguments)[] calls)
        =>
        JsonSerializer.Serialize(
            new
            {
                id = "chatcmpl-1",
                @object = "chat.completion",
                created = 1,
                model = "gpt-test",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        finish_reason = "tool_calls",
                        message = new
                        {
                            role = "assistant",
                            content = (string?)null,
                            tool_calls = calls
                                .Select(static call => new
                                {
                                    id = "call_" + Guid.NewGuid().ToString("N"),
                                    type = "function",
                                    function = new { name = call.Name, arguments = call.Arguments }
                                })
                                .ToArray()
                        }
                    }
                },
                usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 }
            });

    private static string Text(string content)
        =>
        JsonSerializer.Serialize(
            new
            {
                id = "chatcmpl-2",
                @object = "chat.completion",
                created = 1,
                model = "gpt-test",
                choices = new[] { new { index = 0, finish_reason = "stop", message = new { role = "assistant", content } } },
                usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 }
            });

    private sealed class FlowContext
    {
        internal FlowContext(params string[] modelResponses)
        {
            var responses = new Queue<string>(modelResponses);
            Model = new(_ => FakeHttpMessageHandler.Json(responses.Dequeue()));

            Api = new(
                request => request.RequestUri!.AbsolutePath.EndsWith("swagger.json", StringComparison.Ordinal)
                    ? FakeHttpMessageHandler.Json(Specification)
                    : request.RequestUri.AbsolutePath.EndsWith("searchProjects", StringComparison.Ordinal)
                        ? FakeHttpMessageHandler.Json($$"""{"isSuccess":true,"projects":[{"id":"{{ProjectId}}","name":"E2E Role test","type":"Project"}]}""")
                        : FakeHttpMessageHandler.Json(
                            """{"isSuccess":true}""",
                            FailingBodyText is not null && request.Content!.ReadAsStringAsync().Result.Contains(FailingBodyText, StringComparison.Ordinal)
                                ? System.Net.HttpStatusCode.InternalServerError
                                : ApiStatusCode));

            var tool = new AgentToolOption("Timesheet", AgentToolType.OpenApi, new("https://apim.example/swagger/timesheet/swagger.json"))
            {
                BaseAddress = new("https://apim.example/timesheet/"),
                Audience = "api://timesheet",
                NoConfirmationOperations = ["search-projects", "update-profile"]
            };

            Service = new(
                new OpenAiKernelFactory(Model),
                new AgentToolLoader([tool], Api, new FakeAgentUserTokenProvider("user-token"), NullLoggerFactory.Instance),
                History,
                Actions,
                new AgentEntityCache(),
                new("system prompt", TimeZoneInfo.Utc, Time),
                new(
                    new(TimeZoneInfo.Utc, MaxTextLength: 2000, MaxHistoryMessageCount: 20),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Timesheet.search-projects", "Timesheet.update-profile" },
                    TimeSpan.FromMinutes(10))
                {
                    UserSettingsFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Timesheet.update-profile" }
                },
                Time,
                NullLogger.Instance);
        }

        internal FakeHttpMessageHandler Model { get; }

        internal FakeHttpMessageHandler Api { get; }

        internal System.Net.HttpStatusCode ApiStatusCode { get; set; } = System.Net.HttpStatusCode.OK;

        // A request whose body contains this text fails, the other requests succeed
        internal string? FailingBodyText { get; set; }

        internal InMemoryAgentActionStore Actions { get; } = new();

        internal RecordingHistoryStore History { get; } = new();

        internal MutableTime Time { get; } = new();

        internal AgentChatService Service { get; }

        internal string[] ApiPaths
            =>
            [.. Api.Requests.Select(static request => request.Uri.AbsolutePath).Where(static path => path.EndsWith("swagger.json", StringComparison.Ordinal) is false)];
    }

    private sealed class MutableTime : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
            =>
            Now;
    }

    private sealed class OpenAiKernelFactory(HttpMessageHandler handler) : IAgentKernelFactory
    {
        public Kernel Create()
        {
            var builder = Kernel.CreateBuilder();
            _ = builder.AddOpenAIChatCompletion("gpt-test", "test-key", httpClient: new HttpClient(handler, disposeHandler: false));

            return builder.Build();
        }
    }

    private sealed class RecordingHistoryStore : IAgentHistoryStore
    {
        internal List<(string UserText, string AssistantText)> Turns { get; } = [];

        public ValueTask<IReadOnlyList<AgentHistoryMessage>> GetAsync(
            AgentUserContext user, int maxCount, CancellationToken cancellationToken)
            =>
            new(
                Turns.SelectMany<(string UserText, string AssistantText), AgentHistoryMessage>(
                    static turn => [new(true, turn.UserText), new(false, turn.AssistantText)]).ToArray());

        public ValueTask AppendAsync(
            AgentUserContext user, string userText, string assistantText, int keepCount, CancellationToken cancellationToken)
        {
            Turns.Add((userText, assistantText));
            return default;
        }
    }
}
