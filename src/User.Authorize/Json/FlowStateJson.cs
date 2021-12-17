using System;
using System.Text.Json.Serialization;
using Microsoft.Bot.Builder.Dialogs;

namespace GGroupp.Internal.Timesheet;

internal readonly record struct FlowStateJson
{
    [JsonPropertyName("expirationDate")]
    public DateTimeOffset ExpirationDate { get; init; }

    public OAuthPromptSettings? Settings { get; init; }
}