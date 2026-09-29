using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentMessageApiTest
{
    [Fact]
    public static async Task SendMessageAsync_ExpectRequestAndMappedMessage()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://example.com/internal/agent/messages", request.RequestUri?.AbsoluteUri);

            var content = request.Content;
            Assert.NotNull(content);

            var body = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("\"telegramUpdateId\":101", body, StringComparison.Ordinal);
            Assert.Contains("\"telegramUserId\":202", body, StringComparison.Ordinal);
            Assert.Contains("\"telegramChatId\":303", body, StringComparison.Ordinal);
            Assert.Contains("\"text\":\"Some question\"", body, StringComparison.Ordinal);
            Assert.Contains("\"locale\":\"ru\"", body, StringComparison.Ordinal);

            return new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "text": "Some response",
                      "preparedAction": {
                        "actionId": "78302d93-e6dc-4fd6-be63-2480c8984382",
                        "date": "2026-09-29",
                        "projectId": "2b36961d-7645-4e74-a87d-f309acd84f09",
                        "projectName": "Some project",
                        "projectType": 1,
                        "duration": 2.5,
                        "description": "Some work",
                        "expiresAt": "2026-09-29T12:00:00+00:00"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });

        using var client = new HttpClient(handler) { BaseAddress = new("https://example.com/") };
        var actual = await new AgentMessageApi(client).SendMessageAsync(
            101,
            202,
            303,
            "Some question",
            "ru",
            TestContext.Current.CancellationToken);

        Assert.Equal(
            new AgentMessage(
                "Some response",
                new(
                    new("78302d93-e6dc-4fd6-be63-2480c8984382"),
                    new(2026, 9, 29),
                    new("2b36961d-7645-4e74-a87d-f309acd84f09"),
                    "Some project",
                    1,
                    2.5m,
                    "Some work",
                    new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero))),
            actual);
    }

    [Fact]
    public static async Task SendMessageAsync_ResponseIsConflict_ExpectApiException()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(static _ => new(HttpStatusCode.Conflict)))
        {
            BaseAddress = new("https://example.com/")
        };

        var exception = await Assert.ThrowsAsync<AgentMessageApiException>(
            async () => await new AgentMessageApi(client).SendMessageAsync(
                101,
                202,
                303,
                "Some question",
                "ru",
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
    }
}
