using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GarageGroup.Internal.Timesheet.AzureFunc.Test;

public static class AgentProfileApiTest
{
    [Fact]
    public static async Task GetProfileAsync_ExpectRequestAndMappedProfile()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://example.com/internal/agent/profile", request.RequestUri?.AbsoluteUri);

            var body = await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("\"telegramUserId\":202", body, StringComparison.Ordinal);
            Assert.Contains("\"telegramChatId\":303", body, StringComparison.Ordinal);

            return new(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"userName\":\"Test User\",\"languageCode\":\"ru\"}", Encoding.UTF8, "application/json")
            };
        });

        using var client = new HttpClient(handler) { BaseAddress = new("https://example.com/") };
        var api = new AgentProfileApi(client);
        var actual = await api.GetProfileAsync(202, 303, TestContext.Current.CancellationToken);

        Assert.Equal(new AgentProfile("Test User", "ru"), actual);
    }

    [Fact]
    public static async Task GetProfileAsync_ResponseIsForbidden_ExpectApiException()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(static _ => new(HttpStatusCode.Forbidden)))
        {
            BaseAddress = new("https://example.com/")
        };

        var api = new AgentProfileApi(client);
        var exception = await Assert.ThrowsAsync<AgentProfileApiException>(
            async () => await api.GetProfileAsync(202, 202, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
    }
}
