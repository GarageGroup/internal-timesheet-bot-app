using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GarageGroup.Internal.Timesheet;

internal sealed class AgentActionApi(HttpClient httpClient) : IAgentActionApi
{
    private static readonly JsonSerializerOptions ResponseSerializerOptions
        =
        new(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

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
            var problem = await ReadProblemAsync(response.Content, cancellationToken).ConfigureAwait(false);

            throw new AgentMessageApiException(
                response.StatusCode,
                problem?.Title,
                problem?.Detail ?? problem?.FailureMessage,
                problem?.FailureCode);
        }

        var result = await HttpContentJsonExtensions.ReadFromJsonAsync<AgentActionDecisionOut>(
            response.Content,
            ResponseSerializerOptions,
            cancellationToken).ConfigureAwait(false);

        return result ?? throw new InvalidOperationException("Agent API returned an empty action decision response.");
    }

    private sealed record class AgentActionDecisionRequest(
        long TelegramUpdateId,
        long TelegramUserId,
        long TelegramChatId,
        AgentActionDecision Decision);

    private static async ValueTask<AgentApiProblem?> ReadProblemAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;

            return new(
                GetString(root, "title"),
                GetString(root, "detail"),
                GetFailureCode(root),
                GetString(root, "failureMessage"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var property) is false || property.ValueKind is not JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    private static string? GetFailureCode(JsonElement root)
    {
        if (root.TryGetProperty("failureCode", out var property) is false)
        {
            return null;
        }

        if (property.ValueKind is JsonValueKind.String)
        {
            return property.GetString();
        }

        if (property.ValueKind is not JsonValueKind.Number || property.TryGetInt32(out var code) is false)
        {
            return null;
        }

        return code switch
        {
            1 => "InvalidIdentity",
            2 => "UserNotLinked",
            3 => "UserUnavailable",
            4 => "WriteDisabled",
            5 => "InvalidDecision",
            6 => "ActionNotFound",
            7 => "ActionExpired",
            8 => "InvalidActionState",
            9 => "ActionConflict",
            10 => "InvalidTimesheet",
            11 => "TimesheetForbidden",
            12 => "ProjectNotFound",
            13 => "Indeterminate",
            _ => null
        };
    }
}
