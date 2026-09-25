using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentProfileApi(HttpClient httpClient) : IAgentProfileApi
{
    public async ValueTask<AgentProfile> GetProfileAsync(
        long telegramUserId,
        long telegramChatId,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClientJsonExtensions.PostAsJsonAsync(
            httpClient,
            "internal/agent/profile",
            new AgentProfileRequest(telegramUserId, telegramChatId),
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode is false)
        {
            throw new AgentProfileApiException(response.StatusCode);
        }

        var profile = await HttpContentJsonExtensions.ReadFromJsonAsync<AgentProfile>(
            response.Content,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return profile ?? throw new InvalidOperationException("Agent API returned an empty profile response.");
    }

    private sealed record class AgentProfileRequest(long TelegramUserId, long TelegramChatId);
}
