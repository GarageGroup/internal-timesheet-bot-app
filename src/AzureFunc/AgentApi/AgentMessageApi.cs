using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentMessageApi(HttpClient httpClient) : IAgentMessageApi
{
    public async ValueTask<AgentMessage> SendMessageAsync(
        int telegramUpdateId,
        long telegramUserId,
        long telegramChatId,
        string text,
        string locale,
        CancellationToken cancellationToken)
    {
        return await SendAsync(
            new AgentMessageRequest(telegramUpdateId, telegramUserId, telegramChatId, text, locale),
            cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<AgentMessage> SendVoiceMessageAsync(
        int telegramUpdateId,
        long telegramUserId,
        long telegramChatId,
        byte[] audio,
        string mimeType,
        string fileName,
        string language,
        string locale,
        CancellationToken cancellationToken)
        =>
        SendAsync(
            new AgentMessageRequest(
                telegramUpdateId,
                telegramUserId,
                telegramChatId,
                string.Empty,
                locale,
                Convert.ToBase64String(audio),
                mimeType,
                fileName,
                language),
            cancellationToken);

    private async ValueTask<AgentMessage> SendAsync(
        AgentMessageRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await HttpClientJsonExtensions.PostAsJsonAsync(
            httpClient,
            "internal/agent/messages",
            request,
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode is false)
        {
            throw new AgentMessageApiException(response.StatusCode);
        }

        var message = await HttpContentJsonExtensions.ReadFromJsonAsync<AgentMessage>(
            response.Content,
            cancellationToken).ConfigureAwait(false);

        return message ?? throw new InvalidOperationException("Agent API returned an empty message response.");
    }

    private sealed record class AgentMessageRequest(
        int TelegramUpdateId,
        long TelegramUserId,
        long TelegramChatId,
        string Text,
        string Locale,
        string AudioBase64 = "",
        string AudioMimeType = "",
        string AudioFileName = "",
        string AudioLanguage = "");
}
