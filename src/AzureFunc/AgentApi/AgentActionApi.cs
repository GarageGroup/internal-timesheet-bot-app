using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentActionApi(HttpClient httpClient) : IAgentActionApi
{
    public async ValueTask<AgentActionDecisionOut> DecideAsync(
        Guid actionId,
        long telegramUpdateId,
        long telegramUserId,
        long telegramChatId,
        AgentActionDecision decision,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClientJsonExtensions.PostAsJsonAsync(
            httpClient,
            $"internal/agent/actions/{actionId:D}/decision",
            new AgentActionDecisionRequest(telegramUpdateId, telegramUserId, telegramChatId, decision),
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode is false)
        {
            throw new AgentMessageApiException(response.StatusCode);
        }

        var result = await HttpContentJsonExtensions.ReadFromJsonAsync<AgentActionDecisionOut>(
            response.Content,
            cancellationToken).ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Agent API returned an empty action decision response.");
    }

    private sealed record class AgentActionDecisionRequest(
        long TelegramUpdateId,
        long TelegramUserId,
        long TelegramChatId,
        AgentActionDecision Decision);
}
