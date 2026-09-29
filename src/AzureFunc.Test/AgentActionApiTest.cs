using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentActionApiTest
{
    private static readonly Guid SomeActionId = new("78302d93-e6dc-4fd6-be63-2480c8984382");

    [Fact]
    public static async Task DecideAsync_ExpectRequestAndMappedDecision()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                $"https://example.com/internal/agent/actions/{SomeActionId:D}/decision",
                request.RequestUri?.AbsoluteUri);

            var content = request.Content;
            Assert.NotNull(content);

            var body = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("\"telegramUpdateId\":101", body, StringComparison.Ordinal);
            Assert.Contains("\"telegramUserId\":202", body, StringComparison.Ordinal);
            Assert.Contains("\"telegramChatId\":303", body, StringComparison.Ordinal);
            Assert.Contains("\"decision\":0", body, StringComparison.Ordinal);

            return new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"actionId\":\"{SomeActionId:D}\",\"decision\":0}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        using var client = new HttpClient(handler) { BaseAddress = new("https://example.com/") };
        var actual = await new AgentActionApi(client).DecideAsync(
            SomeActionId,
            101,
            202,
            303,
            AgentActionDecision.Confirm,
            TestContext.Current.CancellationToken);

        Assert.Equal(new AgentActionDecisionOut(SomeActionId, AgentActionDecision.Confirm), actual);
    }

    [Fact]
    public static async Task DecideAsync_ResponseIsConflict_ExpectApiException()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(static _ => new(HttpStatusCode.Conflict)))
        {
            BaseAddress = new("https://example.com/")
        };

        var exception = await Assert.ThrowsAsync<AgentMessageApiException>(
            async () => await new AgentActionApi(client).DecideAsync(
                SomeActionId,
                101,
                202,
                303,
                AgentActionDecision.Cancel,
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }
}
